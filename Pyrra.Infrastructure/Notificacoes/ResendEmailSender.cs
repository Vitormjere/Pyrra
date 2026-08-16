using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pyrra.Application.Notificacoes.Email;

namespace Pyrra.Infrastructure.Notificacoes {
    // fica na Infrastructure porque fala HTTP com a API da Resend — Application só conhece
    // IEmailSender, mesmo padrão do HCaptchaVerificationService/AnthropicZeloAssistant
    public class ResendEmailSender : IEmailSender {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ResendSettings     _settings;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(IHttpClientFactory httpClientFactory, IOptions<ResendSettings> settings, ILogger<ResendEmailSender> logger) {
            _httpClientFactory = httpClientFactory;
            _settings          = settings.Value;
            _logger            = logger;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default) {
            var payload = new {
                from    = $"{_settings.FromName} <{_settings.FromEmail}>",
                to      = new[] { toEmail },
                subject,
                html    = htmlBody
            };

            using var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("ResendClient");

            try {
                var response = await client.PostAsync("emails", body, cancellationToken);
                if (!response.IsSuccessStatusCode) {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Resend respondeu {StatusCode} ao enviar e-mail para {ToEmail}: {ResponseBody}",
                        (int)response.StatusCode, toEmail, responseBody);
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception ex) {
                // timeout, DNS, etc. — mesmo raciocínio de nunca deixar o e-mail derrubar quem o disparou
                _logger.LogError(ex, "Falha ao enviar e-mail via Resend para {ToEmail}.", toEmail);
            }
        }
    }
}
