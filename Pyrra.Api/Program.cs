using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
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
using Pyrra.Api.HostedServices;
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

// Azure SQL Serverless auto-pausa e a primeira conexão que acorda ele costuma falhar.
// Pyrra.Api.Tests (WebApplicationFactory) registra o próprio DbContext em memória pro
// ambiente "Testing" — pular aqui evita registrar o provider SQL Server nesse caso, que
// senão colide com o provider InMemory do teste (EF Core não aceita dois providers no
// mesmo container de DI).
if (!builder.Environment.IsEnvironment("Testing")) {
    builder.Services.AddDbContext<PyrraDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null)));
}

// Lê os origins da configuração e falha se não existirem.
// PRODUÇÃO: os origins vêm de configuração externa (nunca commitada) — no App Service,
// em Configuration -> Application settings, como Cors__AllowedOrigins__0 /
// Cors__AllowedOrigins__1 (o "__" é como o ASP.NET Core mapeia variável de ambiente pra
// chave de config aninhada "Cors:AllowedOrigins:0" etc). CONFERIR que sejam exatamente:
//   Cors__AllowedOrigins__0 = https://pyrra.com.br
//   Cors__AllowedOrigins__1 = https://www.pyrra.com.br
// e nada além disso (sem localhost, sem wildcard, sem domínio de preview do Vercel).
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

// Login e registro são os alvos mais comuns de brute force/spam de bots — sem limite, um
// atacante pode tentar senha ilimitadamente contra qualquer e-mail. Uma política POR
// endpoint (não uma compartilhada) pra tentativas de login não consumirem a cota de
// registro e vice-versa. Sliding window (não fixed window) pra não deixar um cliente
// dobrar o limite efetivo bem na borda entre duas janelas — com fixed window, 10 no
// segundo 59 + 10 no segundo 61 dão 20 requisições em 2s; sliding window suaviza isso
// dividindo a janela em segmentos que vão expirando aos poucos.
var authRateLimitConfig = builder.Configuration.GetSection("RateLimiting:Auth");
var authPermitLimit     = authRateLimitConfig.GetValue("PermitLimit", 10);
var authWindowSeconds   = authRateLimitConfig.GetValue("WindowSeconds", 60);
var authSegments        = authRateLimitConfig.GetValue("SegmentsPerWindow", 4);

// chave de particionamento: IP real do cliente — depende do ForwardedHeaders configurado
// mais abaixo pra funcionar atrás do proxy reverso do Azure App Service (senão todo mundo
// cairia na mesma partição, o IP interno do proxy). Reaproveitada pela política geral abaixo.
static string RateLimitPartitionKey(HttpContext httpContext) =>
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

// Política geral, aplicada a toda a API por padrão (GlobalLimiter roda em cima de qualquer
// política por endpoint, não no lugar dela — login/registro continuam com a cota mais
// apertada deles além dessa). 150 req/min por IP: folgada pro uso normal de uma pessoa
// (navegar entre telas, abrir o dia, registrar treino/refeição/gasto) que fica na casa de
// poucas requisições por segundo em rajada, mas barra scraping/abuso automatizado óbvio.
var generalRateLimitConfig = builder.Configuration.GetSection("RateLimiting:General");
var generalPermitLimit     = generalRateLimitConfig.GetValue("PermitLimit", 150);
var generalWindowSeconds   = generalRateLimitConfig.GetValue("WindowSeconds", 60);
var generalSegments        = generalRateLimitConfig.GetValue("SegmentsPerWindow", 6);

builder.Services.AddRateLimiter(options => {
    options.OnRejected = async (context, cancellationToken) => {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        // o sliding window limiter nem sempre anexa a métrica de RetryAfter na rejeição
        // (não acontece quando QueueLimit é 0, por exemplo) — sem ela, cai pro tempo de um
        // segmento da janela, que é quando a próxima vaga tende a abrir de qualquer forma
        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)retryAfter.TotalSeconds
            : Math.Max(1, authWindowSeconds / authSegments);
        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Muitas tentativas. Aguarde um instante e tente novamente." },
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: RateLimitPartitionKey(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions {
                PermitLimit       = generalPermitLimit,
                Window            = TimeSpan.FromSeconds(generalWindowSeconds),
                SegmentsPerWindow = generalSegments,
                QueueLimit        = 0
            }));

    options.AddPolicy("AuthLogin", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: RateLimitPartitionKey(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions {
                PermitLimit       = authPermitLimit,
                Window            = TimeSpan.FromSeconds(authWindowSeconds),
                SegmentsPerWindow = authSegments,
                QueueLimit        = 0
            }));

    options.AddPolicy("AuthRegister", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: RateLimitPartitionKey(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions {
                PermitLimit       = authPermitLimit,
                Window            = TimeSpan.FromSeconds(authWindowSeconds),
                SegmentsPerWindow = authSegments,
                QueueLimit        = 0
            }));
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<CaptchaSettings>(builder.Configuration.GetSection("Captcha"));

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

// CAPTCHA no cadastro — hCaptcha: token conferido no siteverify antes de criar a conta
builder.Services.AddHttpClient("HCaptchaClient", client => {
    client.BaseAddress = new Uri("https://hcaptcha.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ICaptchaVerificationService, HCaptchaVerificationService>();

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
builder.Services.AddScoped<IAchievementService, AchievementService>();

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
builder.Services.AddScoped<IUserProfilePictureStorageService, AzureBlobUserProfilePictureStorageService>();
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

// Chat em grupo por time
builder.Services.AddScoped<ITeamChatMessageRepository, TeamChatMessageRepository>();
builder.Services.AddScoped<ITeamChatService, TeamChatService>();

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
builder.Services.AddScoped<ITeamDailyChallengeRepository, TeamDailyChallengeRepository>();
builder.Services.AddScoped<IChallengeSubmissionRepository, ChallengeSubmissionRepository>();
builder.Services.AddScoped<ITeamMemberScoreRepository, TeamMemberScoreRepository>();
builder.Services.AddScoped<IChallengeSubmissionStorageService, AzureBlobChallengeSubmissionStorageService>();
builder.Services.AddScoped<ITeamChallengeService, TeamChallengeService>();

// Sorteio diário dos 3 desafios por time (Fase 3) — job roda a cada 15min, ver
// DailyChallengeGeneratorHostedService
builder.Services.AddScoped<IDailyChallengeGeneratorService, DailyChallengeGeneratorService>();
builder.Services.AddHostedService<DailyChallengeGeneratorHostedService>();

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

// Zelo conversacional (Treino + Nutrição): cliente HTTP próprio, mesma API mas com timeout maior —
// gera um plano de 7 dias em JSON (até 4000 tokens), a pergunta rápida do Zelo geral não precisa disso
builder.Services.AddHttpClient("AnthropicPlanClient", client => {
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", builder.Configuration["Anthropic:ApiKey"] ?? string.Empty);
    client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", "2023-06-01");
});

builder.Services.AddScoped<IZeloPlanSessionRepository, ZeloPlanSessionRepository>();
builder.Services.AddScoped<IZeloPlanAnswerRepository, ZeloPlanAnswerRepository>();
builder.Services.AddScoped<IZeloPlanMessageRepository, ZeloPlanMessageRepository>();
builder.Services.AddScoped<IZeloPlanQueryLogRepository, ZeloPlanQueryLogRepository>();
builder.Services.AddScoped<IZeloPlanAssistant, AnthropicZeloPlanAssistant>();
builder.Services.AddScoped<IZeloPlanService, ZeloPlanService>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

// Azure App Service fica atrás de um proxy reverso: sem isso, RemoteIpAddress seria
// sempre o IP interno do proxy, não o do cliente real — quebraria o particionamento por
// IP do rate limiter acima (todo mundo cairia na mesma partição). KnownNetworks/
// KnownProxies limpos porque o IP de borda do Azure não é fixo/previsível — mesma
// orientação da documentação da Microsoft pra apps hospedados no App Service.
var forwardedHeadersOptions = new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// CORS precisa vir antes do redirecionamento HTTPS
app.UseCors(FrontendCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

// classe parcial pública só pra WebApplicationFactory<Program> conseguir subir a API nos
// testes de integração — programas com top-level statements geram uma Program implícita
// internal, que não dá pra referenciar de fora do assembly
public partial class Program { }