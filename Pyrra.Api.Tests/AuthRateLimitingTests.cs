using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Pyrra.Api.Tests {
    // uma factory só pra classe inteira (mais rápido que subir a API a cada teste); cada
    // teste usa um X-Forwarded-For diferente pra cair numa partição de rate limit própria,
    // então não precisa recriar a factory pra evitar um teste "gastar" a cota do outro
    public class AuthRateLimitingTests : IClassFixture<CustomWebApplicationFactory> {
        private readonly CustomWebApplicationFactory _factory;

        public AuthRateLimitingTests(CustomWebApplicationFactory factory) {
            _factory = factory;
        }

        private HttpClient ClientWithFakeIp(string fakeIp) {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", fakeIp);
            return client;
        }

        private static Task<HttpResponseMessage> PostLogin(HttpClient client, string email = "nao.existe@pyrra.local", string password = "SenhaErrada123") =>
            client.PostAsJsonAsync("/api/auth/login", new { email, password });

        private static Task<HttpResponseMessage> PostRegister(HttpClient client, string email, string password = "SenhaValida123", string name = "Teste") =>
            client.PostAsJsonAsync("/api/auth/register", new { email, password, name });

        [Fact]
        public async Task Login_DentroDoLimite_NuncaRetorna429() {
            var client = ClientWithFakeIp("10.0.0.1");

            for (var i = 0; i < CustomWebApplicationFactory.TestPermitLimit; i++) {
                var response = await PostLogin(client);
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
                // credenciais inválidas mesmo — o que importa aqui é não ser 429
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task Login_ExcedeLimite_RetornaTooManyRequestsComMensagemERetryAfter() {
            var client = ClientWithFakeIp("10.0.0.2");

            for (var i = 0; i < CustomWebApplicationFactory.TestPermitLimit; i++) {
                await PostLogin(client);
            }

            var blocked = await PostLogin(client);

            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
            Assert.True(blocked.Headers.RetryAfter is not null, "Esperava header Retry-After na resposta 429.");

            var body = await blocked.Content.ReadFromJsonAsync<MessageBody>();
            Assert.False(string.IsNullOrWhiteSpace(body?.Message));
        }

        [Fact]
        public async Task Login_ApósJanelaExpirar_VoltaAAceitarRequisicoes() {
            var client = ClientWithFakeIp("10.0.0.3");

            for (var i = 0; i < CustomWebApplicationFactory.TestPermitLimit; i++) {
                await PostLogin(client);
            }
            var blocked = await PostLogin(client);
            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

            // sliding window: espera passar da janela toda (não só um segmento) pra garantir reset
            await Task.Delay(TimeSpan.FromSeconds(CustomWebApplicationFactory.TestWindowSeconds + 1));

            var afterWindow = await PostLogin(client);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, afterWindow.StatusCode);
        }

        [Fact]
        public async Task Register_DentroDoLimite_FuncionaNormalmenteParaUsoLegitimo() {
            var client = ClientWithFakeIp("10.0.0.4");

            var response = await PostRegister(client, "novo.usuario.rate.limit.teste@pyrra.local");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Register_ExcedeLimite_RetornaTooManyRequests() {
            var client = ClientWithFakeIp("10.0.0.5");

            for (var i = 0; i < CustomWebApplicationFactory.TestPermitLimit; i++) {
                // e-mail diferente em cada chamada — o que deve barrar a N+1ª é o rate
                // limit, não a checagem de "e-mail já existe"
                await PostRegister(client, $"spam{i}@pyrra.local");
            }

            var blocked = await PostRegister(client, "spam-extra@pyrra.local");

            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        }

        [Fact]
        public async Task Login_E_Register_TemCotasIndependentes() {
            var client = ClientWithFakeIp("10.0.0.6");

            // estoura a cota de login...
            for (var i = 0; i < CustomWebApplicationFactory.TestPermitLimit; i++) {
                await PostLogin(client);
            }
            var loginBlocked = await PostLogin(client);
            Assert.Equal(HttpStatusCode.TooManyRequests, loginBlocked.StatusCode);

            // ...e confirma que registro no mesmo IP ainda funciona (cota separada)
            var registerStillOk = await PostRegister(client, "independente@pyrra.local");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, registerStillOk.StatusCode);
        }

        private record MessageBody(string? Message);
    }
}
