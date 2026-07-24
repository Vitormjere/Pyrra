using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pyrra.Application.Auth;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Financas;
using Pyrra.Application.Focos;
using Pyrra.Application.Notificacoes;
using Pyrra.Application.Nutricao;
using Pyrra.Application.Planejamento;
using Pyrra.Application.Usuario;
using Pyrra.Application.Streaks;
using Pyrra.Application.Tarefas;
using Pyrra.Application.Treinos;
using Pyrra.Domain.Users;
using Pyrra.Infrastructure.Auth;
using Pyrra.Infrastructure.Common;
using Pyrra.Infrastructure.Data;
using Pyrra.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Enums trafegam como NOME ("Academia"), não como índice. As respostas já faziam isso à mão
// (FocusResponse.Category, UserResponse.Plan, WorkoutResponse.Type usam .ToString()); o converter
// fecha o outro lado do contrato, deixando o corpo da requisição aceitar o mesmo texto que a
// resposta devolve. Sem ele, System.Text.Json só aceitaria o número.
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<PyrraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Origins vêm da configuração, nunca do código: trocar o do frontend em produção é editar o
// appsettings do ambiente, sem recompilar. Falha alto se a seção sumir — uma lista vazia
// registraria uma política que bloqueia tudo silenciosamente, o pior modo de descobrir o erro
// (o navegador só diria "CORS blocked", sem apontar a configuração ausente).
const string FrontendCorsPolicy = "AllowFrontendDev";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0) {
    throw new InvalidOperationException("Seção 'Cors:AllowedOrigins' não encontrada ou vazia em appsettings.json.");
}

builder.Services.AddCors(options => {
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              // AllowCredentials é o que faz o header Authorization atravessar; ele é incompatível
              // com AllowAnyOrigin, então a lista explícita de WithOrigins não é só preferência —
              // é requisito para os dois funcionarem juntos.
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
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

builder.Services.AddAuthorization();

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

builder.Services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();
builder.Services.AddScoped<IWorkoutPlanDayRepository, WorkoutPlanDayRepository>();
builder.Services.AddScoped<IWorkoutPlanExerciseRepository, WorkoutPlanExerciseRepository>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();

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
builder.Services.AddScoped<INightlyMessageService, NightlyMessageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

// ANTES do UseHttpsRedirection, não só antes do UseAuthorization: o preflight OPTIONS que o
// navegador dispara não segue redirecionamento. Se o React chamar o endpoint http (5104), o
// redirect 307 para https mataria o preflight com "Redirect is not allowed for a preflight
// request", antes de qualquer middleware de CORS ser consultado. Aqui o CORS responde o
// preflight e encerra a requisição sem passar pelo redirect.
app.UseCors(FrontendCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
