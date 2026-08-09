using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Treinos;
using Pyrra.Domain.Zelo;

namespace Pyrra.Infrastructure.Zelo {
    // fica na Infrastructure porque fala HTTP, mesmo padrão do AnthropicZeloAssistant. Modelo mais
    // capaz que o Zelo geral (Haiku): aqui a tarefa é montar um plano de 7 dias em JSON estruturado,
    // não uma resposta curta de 2-4 frases.
    public class AnthropicZeloPlanAssistant : IZeloPlanAssistant {
        private const string Model = "claude-sonnet-4-5";
        private const int MaxTokens = 4000;

        // chat livre é pergunta curta sobre um plano já pronto — mesmo modelo/orçamento do Zelo geral, não precisa do Sonnet
        private const string ChatModel = "claude-haiku-4-5";
        private const int ChatMaxTokens = 300;

        private const string FriendlyErrorMessage =
            "O Zelo não conseguiu montar seu plano agora. Tente novamente em alguns instantes.";

        private const string ChatFriendlyErrorMessage =
            "O Zelo está indisponível no momento. Tente novamente em alguns instantes.";

        private static readonly JsonSerializerOptions ResponseJsonOptions = new() {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnthropicZeloPlanAssistant> _logger;

        public AnthropicZeloPlanAssistant(IHttpClientFactory httpClientFactory, ILogger<AnthropicZeloPlanAssistant> logger) {
            _httpClientFactory = httpClientFactory;
            _logger            = logger;
        }

        public async Task<ZeloPlanGenerationResult> GeneratePlanAsync(
            string userContext, IReadOnlyList<ZeloPlanAnswer> answers, CancellationToken cancellationToken = default) {
            var userContent = BuildUserContent(userContext, answers);

            var payload = new {
                model      = Model,
                max_tokens = MaxTokens,
                system     = SystemPrompt,
                messages   = new[] { new { role = "user", content = userContent } }
            };

            using var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("AnthropicPlanClient");

            HttpResponseMessage response;
            try {
                response = await client.PostAsync("v1/messages", body, cancellationToken);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception ex) {
                _logger.LogError(ex, "Falha ao chamar a API da Anthropic para o plano do Zelo.");
                return new ZeloPlanGenerationResult(false, null, FriendlyErrorMessage);
            }

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("API da Anthropic respondeu {StatusCode} para o plano do Zelo.", (int)response.StatusCode);
                return new ZeloPlanGenerationResult(false, null, FriendlyErrorMessage);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            string? text;
            try {
                using var doc = JsonDocument.Parse(json);
                text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            } catch (Exception ex) {
                _logger.LogError(ex, "Não foi possível ler a resposta da Anthropic para o plano do Zelo.");
                return new ZeloPlanGenerationResult(false, null, FriendlyErrorMessage);
            }

            if (string.IsNullOrWhiteSpace(text)) {
                _logger.LogError("Resposta da Anthropic para o plano do Zelo veio sem texto.");
                return new ZeloPlanGenerationResult(false, null, FriendlyErrorMessage);
            }

            var plan = ParsePlan(StripJsonFence(text));
            if (plan is null) {
                _logger.LogError("Resposta da Anthropic para o plano do Zelo não é um JSON válido/completo: {Text}", text);
                return new ZeloPlanGenerationResult(false, null, FriendlyErrorMessage);
            }

            return new ZeloPlanGenerationResult(true, plan, string.Empty);
        }

        public async Task<ZeloAssistantResult> ContinueChatAsync(
            string userContext, GeneratedPlan plan, IReadOnlyList<ZeloPlanMessage> history, string newMessage,
            CancellationToken cancellationToken = default) {
            var systemPrompt = ChatSystemPrompt + "\n\n" + SummarizePlan(plan) + "\n\n" + userContext;

            var messages = history
                .Select(m => new { role = m.Role == ZeloPlanMessageRole.Usuario ? "user" : "assistant", content = m.Content })
                .Append(new { role = "user", content = newMessage })
                .ToArray();

            var payload = new {
                model      = ChatModel,
                max_tokens = ChatMaxTokens,
                system     = systemPrompt,
                messages
            };

            using var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("AnthropicClient");

            HttpResponseMessage response;
            try {
                response = await client.PostAsync("v1/messages", body, cancellationToken);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception ex) {
                _logger.LogError(ex, "Falha ao chamar a API da Anthropic para o chat do Zelo.");
                return new ZeloAssistantResult(false, ChatFriendlyErrorMessage);
            }

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("API da Anthropic respondeu {StatusCode} para o chat do Zelo.", (int)response.StatusCode);
                return new ZeloAssistantResult(false, ChatFriendlyErrorMessage);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            try {
                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                if (string.IsNullOrWhiteSpace(text)) {
                    _logger.LogError("Resposta da Anthropic para o chat do Zelo veio sem texto.");
                    return new ZeloAssistantResult(false, ChatFriendlyErrorMessage);
                }
                return new ZeloAssistantResult(true, text.Trim());
            } catch (Exception ex) {
                _logger.LogError(ex, "Não foi possível ler a resposta da Anthropic para o chat do Zelo.");
                return new ZeloAssistantResult(false, ChatFriendlyErrorMessage);
            }
        }

        // resumo compacto do plano pro prompt do chat — não manda o JSON inteiro pra não estourar o orçamento de tokens à toa
        private static string SummarizePlan(GeneratedPlan plan) {
            var sb = new StringBuilder();
            sb.Append("PLANO ATUAL DO USUÁRIO\n").Append(plan.Summary).AppendLine();

            sb.AppendLine("Treino:");
            foreach (var day in plan.WorkoutDays) {
                var exercises = day.Exercises.Count == 0
                    ? "descanso"
                    : string.Join(", ", day.Exercises.Select(e => e.ExerciseName));
                sb.Append("- ").Append(day.DayOfWeek).Append(": ").AppendLine(exercises);
            }

            sb.AppendLine("Nutrição:");
            foreach (var day in plan.NutritionDays) {
                var items = string.Join(", ", day.Items.Select(i => i.ItemName));
                sb.Append("- ").Append(day.DayOfWeek).Append(": ").AppendLine(items);
            }

            return sb.ToString();
        }

        private const string ChatSystemPrompt =
            "Você é o Zelo, assistente pessoal dentro do app Pyrra. O usuário acabou de receber um " +
            "plano de Treino e Nutrição que você mesmo montou (resumido abaixo). Responda dúvidas e " +
            "pedidos de ajuste sobre esse plano de forma direta, breve (2-4 frases) e encorajadora. " +
            "Você não pode alterar o plano diretamente nesta conversa — se o usuário pedir uma mudança, " +
            "explique a sugestão e diga que ele pode gerar um novo plano se quiser aplicá-la.";

        private static string BuildUserContent(string userContext, IReadOnlyList<ZeloPlanAnswer> answers) {
            var sb = new StringBuilder();
            sb.AppendLine(userContext);
            sb.AppendLine();
            sb.AppendLine("RESPOSTAS DO FORMULÁRIO GUIADO");
            foreach (var answer in answers) {
                sb.Append("- ").Append(answer.Question).Append(' ').AppendLine(answer.Answer);
            }
            return sb.ToString();
        }

        // o modelo às vezes envolve o JSON em ```json apesar da instrução — remove antes de parsear
        private static string StripJsonFence(string text) {
            var trimmed = text.Trim();
            if (!trimmed.StartsWith("```")) {
                return trimmed;
            }

            var firstNewline = trimmed.IndexOf('\n');
            var withoutOpeningFence = firstNewline >= 0 ? trimmed[(firstNewline + 1)..] : trimmed;
            var closingFenceIndex = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);
            return closingFenceIndex >= 0 ? withoutOpeningFence[..closingFenceIndex].Trim() : withoutOpeningFence.Trim();
        }

        // parseia e valida a estrutura, sem confiar cegamente no texto do modelo — qualquer campo
        // ausente, enum desconhecido ou dia faltando derruba a geração inteira (Success = false)
        private static GeneratedPlan? ParsePlan(string json) {
            PlanJson? raw;
            try {
                raw = JsonSerializer.Deserialize<PlanJson>(json, ResponseJsonOptions);
            } catch (JsonException) {
                return null;
            }

            if (raw is null || string.IsNullOrWhiteSpace(raw.Summary)
                || raw.WorkoutDays is null || raw.NutritionDays is null) {
                return null;
            }

            var workoutDays = new List<GeneratedWorkoutDay>();
            foreach (var day in raw.WorkoutDays) {
                if (!Enum.TryParse<WeekDay>(day.DayOfWeek, out var weekDay) || day.Exercises is null) {
                    return null;
                }

                var exercises = new List<GeneratedWorkoutExercise>();
                foreach (var exercise in day.Exercises) {
                    if (!Enum.TryParse<WorkoutType>(exercise.Type, out var type) || string.IsNullOrWhiteSpace(exercise.ExerciseName)) {
                        return null;
                    }
                    exercises.Add(new GeneratedWorkoutExercise(type, exercise.ExerciseName.Trim(), exercise.Sets, exercise.Reps, exercise.Order));
                }

                workoutDays.Add(new GeneratedWorkoutDay(weekDay, string.IsNullOrWhiteSpace(day.Label) ? null : day.Label.Trim(), exercises));
            }

            var nutritionDays = new List<GeneratedNutritionDay>();
            foreach (var day in raw.NutritionDays) {
                if (!Enum.TryParse<WeekDay>(day.DayOfWeek, out var weekDay) || day.Items is null) {
                    return null;
                }

                var items = new List<GeneratedNutritionItem>();
                foreach (var item in day.Items) {
                    if (!Enum.TryParse<MealType>(item.MealType, out var mealType)
                        || string.IsNullOrWhiteSpace(item.ItemName) || string.IsNullOrWhiteSpace(item.Quantity)) {
                        return null;
                    }
                    items.Add(new GeneratedNutritionItem(mealType, item.ItemName.Trim(), item.Quantity.Trim()));
                }

                nutritionDays.Add(new GeneratedNutritionDay(weekDay, items));
            }

            // os 7 dias da semana precisam estar presentes nos dois planos, sem duplicar nenhum
            var expectedDays = Enum.GetValues<WeekDay>().ToHashSet();
            if (workoutDays.Select(d => d.DayOfWeek).ToHashSet().SetEquals(expectedDays) is false
                || nutritionDays.Select(d => d.DayOfWeek).ToHashSet().SetEquals(expectedDays) is false) {
                return null;
            }

            return new GeneratedPlan(raw.Summary.Trim(), workoutDays, nutritionDays);
        }

        private const string SystemPrompt =
            "Você é o Zelo, assistente pessoal dentro do app Pyrra. Monte um plano semanal de Treino " +
            "e Nutrição com base no contexto do usuário e nas respostas do formulário guiado. " +
            "Responda APENAS com um objeto JSON válido, sem markdown, sem texto antes ou depois, " +
            "exatamente neste formato:\n" +
            "{\n" +
            "  \"summary\": \"2-3 frases explicando o plano e o raciocínio por trás dele\",\n" +
            "  \"workoutDays\": [\n" +
            "    { \"dayOfWeek\": \"Segunda\", \"label\": \"Peito e tríceps\" ou null se for descanso,\n" +
            "      \"exercises\": [ { \"type\": \"Academia\", \"exerciseName\": \"Supino reto\", \"sets\": 4, \"reps\": 10, \"order\": 0 } ] }\n" +
            "    // um objeto para cada um dos 7 dias: Segunda, Terca, Quarta, Quinta, Sexta, Sabado, Domingo (sem acento) — " +
            "dia de descanso tem label null e exercises vazio\n" +
            "  ],\n" +
            "  \"nutritionDays\": [\n" +
            "    { \"dayOfWeek\": \"Segunda\",\n" +
            "      \"items\": [ { \"mealType\": \"CafeDaManha\", \"itemName\": \"Ovos mexidos\", \"quantity\": \"3 unidades\" } ] }\n" +
            "    // um objeto para cada um dos 7 dias, mealType é um de: CafeDaManha, Almoco, Lanche, Jantar\n" +
            "  ]\n" +
            "}\n" +
            "Regras: \"type\" de exercício é \"Academia\" ou \"Corrida\" (exatamente assim, sem acento). " +
            "Em \"Corrida\", sets e reps são null e exerciseName descreve o treino (ex.: \"5km leve\", \"tiros 6x400m\"). " +
            "Em \"Academia\", sets e reps são sempre números. Respeite restrições físicas e alimentares informadas " +
            "nas respostas. Os 7 dias da semana devem aparecer exatamente uma vez em workoutDays e em nutritionDays.";

        // formato bruto da resposta do modelo, antes de validar contra os enums do domínio
        private sealed class PlanJson {
            [JsonPropertyName("summary")]
            public string? Summary { get; set; }

            [JsonPropertyName("workoutDays")]
            public List<WorkoutDayJson>? WorkoutDays { get; set; }

            [JsonPropertyName("nutritionDays")]
            public List<NutritionDayJson>? NutritionDays { get; set; }
        }

        private sealed class WorkoutDayJson {
            [JsonPropertyName("dayOfWeek")]
            public string? DayOfWeek { get; set; }

            [JsonPropertyName("label")]
            public string? Label { get; set; }

            [JsonPropertyName("exercises")]
            public List<WorkoutExerciseJson>? Exercises { get; set; }
        }

        private sealed class WorkoutExerciseJson {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("exerciseName")]
            public string? ExerciseName { get; set; }

            [JsonPropertyName("sets")]
            public int? Sets { get; set; }

            [JsonPropertyName("reps")]
            public int? Reps { get; set; }

            [JsonPropertyName("order")]
            public int Order { get; set; }
        }

        private sealed class NutritionDayJson {
            [JsonPropertyName("dayOfWeek")]
            public string? DayOfWeek { get; set; }

            [JsonPropertyName("items")]
            public List<NutritionItemJson>? Items { get; set; }
        }

        private sealed class NutritionItemJson {
            [JsonPropertyName("mealType")]
            public string? MealType { get; set; }

            [JsonPropertyName("itemName")]
            public string? ItemName { get; set; }

            [JsonPropertyName("quantity")]
            public string? Quantity { get; set; }
        }
    }
}
