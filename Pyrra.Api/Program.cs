using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pyrra.Application.Achievements;
using Pyrra.Application.Auth;
using Pyrra.Application.Chat;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Comunidade;
using Pyrra.Application.Desafios;
using Pyrra.Application.Financas;
using Pyrra.Application.Focos;
using Pyrra.Application.Notificacoes;
using Pyrra.Application.Nutricao;
using Pyrra.Application.Planejamento;
using Pyrra.Application.Usuario;
using Pyrra.Application.Streaks;
using Pyrra.Application.Tarefas;
using Pyrra.Application.Treinos;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Users;
using Pyrra.Api.Hubs;
using Pyrra.Infrastructure.Auth;
using Pyrra.Infrastructure.Common;
using Pyrra.Infrastructure.Data;
using Pyrra.Infrastructure.Repositories;
using Pyrra.Infrastructure.Storage;
using Pyrra.Infrastructure.Zelo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// enums trafegam como nome, não índice (as respostas já faziam isso na mão, o converter fecha esse contrato também pro corpo da requisição aceitar o mesmo texto)
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Azure SQL Serverless auto-pausa e a primeira conexão que acorda ele costuma falhar
builder.Services.AddDbContext<PyrraDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Lê os origins da configuração e falha se não existirem
const string FrontendCorsPolicy = "AllowFrontendDev";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0) {
    throw new InvalidOperationException("Seção 'Cors:AllowedOrigins' não encontrada ou vazia em appsettings.json.");
}

builder.Services.AddCors(options => {
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              // Com credenciais, é preciso definir os origins permitidos
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Seção 'Jwt' não encontrada em appsettings.json.");

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer      = jwtSettings.Issuer,
        ValidAudience    = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
    // No Hub, o JWT vem pela query; no resto, pelo Authorization
    options.Events = new JwtBearerEvents {
        OnMessageReceived = context => {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat")) {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IDailyFocusRepository, DailyFocusRepository>();
builder.Services.AddScoped<IDailyFocusService, DailyFocusService>();

builder.Services.AddScoped<IFocusLogRepository, FocusLogRepository>();
builder.Services.AddScoped<IDailyScoreRepository, DailyScoreRepository>();
builder.Services.AddScoped<IDailyScoreCalculator, DailyScoreCalculator>();
builder.Services.AddScoped<IFocusCheckInService, FocusCheckInService>();
builder.Services.AddSingleton<IClockService, SystemClockService>();

builder.Services.AddScoped<IStreakRepository, StreakRepository>();
builder.Services.AddScoped<IFreezeBankRepository, FreezeBankRepository>();
builder.Services.AddScoped<IPendingMilestoneRepository, PendingMilestoneRepository>();
builder.Services.AddScoped<IPendingFreezeUseRepository, PendingFreezeUseRepository>();
builder.Services.AddScoped<IStreakService, StreakService>();

// Conquistas: catálogo fixo (seed) + desbloqueio via streak e desafios aprovados
builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
builder.Services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
builder.Services.AddScoped<IAchievementCheckerService, AchievementCheckerService>();

builder.Services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();
builder.Services.AddScoped<IWorkoutPlanDayRepository, WorkoutPlanDayRepository>();
builder.Services.AddScoped<IWorkoutPlanExerciseRepository, WorkoutPlanExerciseRepository>();
builder.Services.AddScoped<IWorkoutTemplateRepository, WorkoutTemplateRepository>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IWorkoutTemplateService, WorkoutTemplateService>();

builder.Services.AddScoped<IDailyPlanNoteRepository, DailyPlanNoteRepository>();
builder.Services.AddScoped<IDailyPlanNoteService, DailyPlanNoteService>();

builder.Services.AddScoped<IPriorityTaskRepository, PriorityTaskRepository>();
builder.Services.AddScoped<IPriorityTaskService, PriorityTaskService>();

builder.Services.AddScoped<IFinanceCategoryRepository, FinanceCategoryRepository>();
builder.Services.AddScoped<IFinanceEntryRepository, FinanceEntryRepository>();
builder.Services.AddScoped<IFinanceService, FinanceService>();

builder.Services.AddScoped<INutritionEntryRepository, NutritionEntryRepository>();
builder.Services.AddScoped<INutritionPlanItemRepository, NutritionPlanItemRepository>();
builder.Services.AddScoped<INutritionPlanSeedLogRepository, NutritionPlanSeedLogRepository>();
builder.Services.AddScoped<INutritionService, NutritionService>();

builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<IUsernameService, UsernameService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<INightlyMessageService, NightlyMessageService>();
// Serviço de usuários do módulo administrativo
builder.Services.AddScoped<IAdminUserService, AdminUserService>();

// Chat em tempo real entre admin e jogador
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
builder.Services.AddScoped<ITeamInviteRepository, TeamInviteRepository>();
builder.Services.AddScoped<ITeamBannerStorageService, AzureBlobTeamBannerStorageService>();
builder.Services.AddScoped<ITeamService, TeamService>();

// Ranking baseado no streak dos amigos
builder.Services.AddScoped<IRankingService, RankingService>();

// Perfil público com informações de streak
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Serviços administrativos de desafios
builder.Services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();
builder.Services.AddScoped<IChallengeCategoryRepository, ChallengeCategoryRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IChallengeCatalogService, ChallengeCatalogService>();

// Gerencia desafios das equipes
builder.Services.AddScoped<ITeamActiveCategoryRepository, TeamActiveCategoryRepository>();
builder.Services.AddScoped<IChallengeSubmissionRepository, ChallengeSubmissionRepository>();
builder.Services.AddScoped<ITeamMemberScoreRepository, TeamMemberScoreRepository>();
builder.Services.AddScoped<IChallengeSubmissionStorageService, AzureBlobChallengeSubmissionStorageService>();
builder.Services.AddScoped<ITeamChallengeService, TeamChallengeService>();

// Gerencia torneios e aprova solicitações
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<ITournamentRequestRepository, TournamentRequestRepository>();
builder.Services.AddScoped<ITournamentTeamRepository, TournamentTeamRepository>();
builder.Services.AddScoped<ITournamentBannerStorageService, AzureBlobTournamentBannerStorageService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();

// Desafios vinculados aos torneios
builder.Services.AddScoped<ITournamentChallengeRepository, TournamentChallengeRepository>();
builder.Services.AddScoped<ITournamentOwnChallengeRepository, TournamentOwnChallengeRepository>();
builder.Services.AddScoped<ITournamentChallengeService, TournamentChallengeService>();

// Cliente HTTP da API da Anthropic
builder.Services.AddHttpClient("AnthropicClient", client => {
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", builder.Configuration["Anthropic:ApiKey"] ?? string.Empty);
    client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", "2023-06-01");
});

builder.Services.AddScoped<IZeloQueryLogRepository, ZeloQueryLogRepository>();
builder.Services.AddScoped<IZeloContextBuilder, ZeloContextBuilder>();
builder.Services.AddScoped<IZeloAssistant, AnthropicZeloAssistant>();
builder.Services.AddScoped<IZeloService, ZeloService>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

// CORS precisa vir antes do redirecionamento HTTPS
app.UseCors(FrontendCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();