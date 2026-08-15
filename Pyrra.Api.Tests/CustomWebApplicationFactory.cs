using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pyrra.Application.Auth;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Api.Tests {
    // sobe a API de verdade (Program.cs real, DI real) só trocando o banco por InMemory —
    // os testes de rate limiting não precisam de SQL Server/LocalDB, só de um contexto que
    // responda. Janela/limite bem menores que o de produção pra não deixar os testes lentos.
    //
    // Overrides via variável de ambiente (não via ConfigureAppConfiguration): Program.cs usa
    // top-level statements com WebApplication.CreateBuilder(args), que já lê
    // builder.Configuration logo nas primeiras linhas (Jwt, Cors) — o hook
    // ConfigureAppConfiguration do WebApplicationFactory roda tarde demais pra isso. Variável
    // de ambiente é lida pelo AddEnvironmentVariables() padrão do CreateBuilder, então já
    // está disponível a tempo. Mesma convenção "__" usada de verdade no App Service.
    public class CustomWebApplicationFactory : WebApplicationFactory<Program> {
        public const int TestPermitLimit   = 3;
        public const int TestWindowSeconds = 2;

        public CustomWebApplicationFactory() {
            Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", TestPermitLimit.ToString());
            Environment.SetEnvironmentVariable("RateLimiting__Auth__WindowSeconds", TestWindowSeconds.ToString());
            Environment.SetEnvironmentVariable("RateLimiting__Auth__SegmentsPerWindow", "2");
            Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:5173");
            // chave só pra teste, nunca usada fora do TestServer em memória
            Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-32-chars-minimum-for-hmacsha256");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "Pyrra.Api");
            Environment.SetEnvironmentVariable("Jwt__Audience", "Pyrra.Client");
            Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            // Program.cs pula o AddDbContext(SqlServer) quando o ambiente é "Testing" — só
            // precisa adicionar o InMemory aqui, sem provider nenhum pra remover antes.
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services => {
                services.AddDbContext<PyrraDbContext>(options =>
                    options.UseInMemoryDatabase("AuthRateLimitTests"));

                // troca a verificação real do hCaptcha (rede, precisa de secret key) por uma
                // que sempre aprova — os testes daqui não avaliam CAPTCHA, só rate limit/lockout
                services.RemoveAll<ICaptchaVerificationService>();
                services.AddScoped<ICaptchaVerificationService, AlwaysPassCaptchaVerificationService>();
            });
        }
    }

    internal sealed class AlwaysPassCaptchaVerificationService : ICaptchaVerificationService {
        public Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
