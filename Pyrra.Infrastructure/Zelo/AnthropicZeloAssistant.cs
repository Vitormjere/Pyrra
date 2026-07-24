using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pyrra.Application.Zelo;

namespace Pyrra.Infrastructure.Zelo {
    // Implementação da fronteira com a Anthropic. Vive na Infrastructure porque fala HTTP; a
    // Application só conhece a interface IZeloAssistant, mesmo padrão do JwtTokenService.
    public class AnthropicZeloAssistant : IZeloAssistant {
        // O identificador do Haiku mais recente. Barato e rápido, o suficiente para respostas
        // curtas de 2 a 4 frases.
        private const string Model = "claude-haiku-4-5";

        // Respostas curtas por decisão de produto: teto baixo de tokens segura custo e latência.
        private const int MaxTokens = 300;

        private const string SystemPrompt =
            "Você é o Zelo, assistente pessoal dentro do app Pyrra. Responda de forma direta, " +
            "breve (2-4 frases) e encorajadora, baseado apenas nos dados fornecidos. Se não houver " +
            "dado suficiente para responder algo, diga isso honestamente.";

        // Mensagem única para qualquer falha (timeout, 4xx/5xx, resposta ilegível): o usuário nunca
        // vê detalhe técnico, só um convite a tentar de novo.
        private const string FriendlyErrorMessage =
            "O Zelo está indisponível no momento. Tente novamente em alguns instantes.";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnthropicZeloAssistant> _logger;

        public AnthropicZeloAssistant(IHttpClientFactory httpClientFactory, ILogger<AnthropicZeloAssistant> logger) {
            _httpClientFactory = httpClientFactory;
            _logger            = logger;
        }

        public async Task<ZeloAssistantResult> AskAsync(string question, string context, CancellationToken cancellationToken = default) {
            var userContent = $"{context}\n\nPergunta do usuário: {question}";

            var payload = new {
                model      = Model,
                max_tokens = MaxTokens,
                system     = SystemPrompt,
                messages   = new[] { new { role = "user", content = userContent } }
            };

            using var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("AnthropicClient");

            HttpResponseMessage response;
            try {
                response = await client.PostAsync("v1/messages", body, cancellationToken);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // Cancelamento vindo do cliente (ex.: usuário saiu da tela) não é falha nossa —
                // deixa propagar em vez de virar mensagem amigável.
                throw;
            } catch (Exception ex) {
                // Timeout (o HttpClient cancela por conta própria, sem o token do chamador) ou falha
                // de rede caem aqui.
                _logger.LogError(ex, "Falha ao chamar a API da Anthropic para o Zelo.");
                return new ZeloAssistantResult(false, FriendlyErrorMessage);
            }

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("API da Anthropic respondeu {StatusCode} para o Zelo.", (int)response.StatusCode);
                return new ZeloAssistantResult(false, FriendlyErrorMessage);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            try {
                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                if (string.IsNullOrWhiteSpace(text)) {
                    _logger.LogError("Resposta da Anthropic para o Zelo veio sem texto.");
                    return new ZeloAssistantResult(false, FriendlyErrorMessage);
                }
                return new ZeloAssistantResult(true, text.Trim());
            } catch (Exception ex) {
                _logger.LogError(ex, "Não foi possível ler a resposta da Anthropic para o Zelo.");
                return new ZeloAssistantResult(false, FriendlyErrorMessage);
            }
        }
    }
}
