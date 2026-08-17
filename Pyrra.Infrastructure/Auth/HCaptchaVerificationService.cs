using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pyrra.Application.Auth;

namespace Pyrra.Infrastructure.Auth {
    // fica na Infrastructure porque fala HTTP — Application só conhece ICaptchaVerificationService,
    // mesmo padrão do JwtTokenService e do AnthropicZeloAssistant
    public class HCaptchaVerificationService : ICaptchaVerificationService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CaptchaSettings    _settings;
        private readonly ILogger<HCaptchaVerificationService> _logger;

        public HCaptchaVerificationService(
            IHttpClientFactory                   httpClientFactory,
            IOptions<CaptchaSettings>            settings,
            ILogger<HCaptchaVerificationService> logger) {
            _httpClientFactory = httpClientFactory;
            _settings          = settings.Value;
            _logger            = logger;
        }

        public async Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(token)) {
                return false;
            }

            var client = _httpClientFactory.CreateClient("HCaptchaClient");

            using var body = new FormUrlEncodedContent(new Dictionary<string, string> {
                ["secret"]   = _settings.SecretKey,
                ["response"] = token
            });

            HttpResponseMessage response;
            try {
                response = await client.PostAsync("siteverify", body, cancellationToken);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception ex) {
                // provedor fora do ar/timeout: falha fechada — deixar passar sem confirmação
                // derrubaria a própria proteção que o CAPTCHA existe pra dar
                _logger.LogError(ex, "Falha ao chamar a API de verificação do hCaptcha.");
                return false;
            }

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("API do hCaptcha respondeu {StatusCode}.", (int)response.StatusCode);
                return false;
            }

            try {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var success = doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

                // 200 com success:false é resposta normal do hCaptcha (não erro de rede/HTTP),
                // então cai fora do catch acima — sem logar aqui, "por que" a verificação falhou
                // fica invisível (só se sabe que falhou, não a causa: token expirado, secret/site
                // key incompatíveis, token repetido etc.)
                if (!success) {
                    var errorCodes = doc.RootElement.TryGetProperty("error-codes", out var codes) ? codes.ToString() : "(nenhum)";
                    _logger.LogWarning("hCaptcha recusou o token — error-codes: {ErrorCodes}", errorCodes);
                }

                return success;
            } catch (Exception ex) {
                _logger.LogError(ex, "Não foi possível ler a resposta do hCaptcha.");
                return false;
            }
        }
    }
}
