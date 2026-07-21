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
using Pyrra.Application.Nutricao;
using Pyrra.Application.Planejamento;
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
builder.Services.AddScoped<IStreakService, StreakService>();

builder.Services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();

builder.Services.AddScoped<IDailyPlanNoteRepository, DailyPlanNoteRepository>();
builder.Services.AddScoped<IDailyPlanNoteService, DailyPlanNoteService>();

builder.Services.AddScoped<IPriorityTaskRepository, PriorityTaskRepository>();
builder.Services.AddScoped<IPriorityTaskService, PriorityTaskService>();

builder.Services.AddScoped<IFinanceCategoryRepository, FinanceCategoryRepository>();
builder.Services.AddScoped<IFinanceEntryRepository, FinanceEntryRepository>();
builder.Services.AddScoped<IFinanceService, FinanceService>();

builder.Services.AddScoped<INutritionEntryRepository, NutritionEntryRepository>();
builder.Services.AddScoped<INutritionService, NutritionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
