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

        // 7 dias de treino + 7 dias de nutrição em JSON detalhado passa fácil de 4000 tokens — visto
        // na prática cortando a resposta no meio do JSON (achado testando ao vivo)
        private const int MaxTokens = 8000;

        // chat livre é pergunta curta sobre um plano já pronto — mesmo modelo do Zelo geral, não
        // precisa do Sonnet. O orçamento de tokens é maior que o do Zelo geral (300): quando a
        // sessão permite edição, a resposta carrega o envelope JSON inteiro (reply + editProposal
        // com a lista completa de exercícios/itens do dia) — 300 tokens truncava esse JSON no meio
        // e derrubava o parse (achado testando ao vivo).
        private const string ChatModel = "claude-haiku-4-5";
        private const int ChatMaxTokens = 1024;

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

            // ReadAsStringAsync() decide o charset pelo Content-Type da resposta e, sem um charset
            // explícito, acabou lendo como Latin-1 na prática — acentos em português saíam
            // duplamente corrompidos (achado testando ao vivo). A API da Anthropic sempre responde
            // em UTF-8, então força a decodificação em vez de confiar na detecção automática.
            var jsonBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var json      = Encoding.UTF8.GetString(jsonBytes);
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

            var stripped = StripJsonFence(text);
            var plan = ParsePlan(stripped);
            if (plan is null) {
                _logger.LogError("Resposta da Anthropic para o plano do Zelo não é um JSON válido/completo: {Text}", stripped);
                return new ZeloPlanGenerationResult(false, null, FriendlyErrorMessage);
            }

            return new ZeloPlanGenerationResult(true, plan, string.Empty);
        }

        public async Task<ZeloChatContinuationResult> ContinueChatAsync(
            string userContext, GeneratedPlan plan, IReadOnlyList<ZeloPlanMessage> history, string newMessage,
            bool allowEdits, CancellationToken cancellationToken = default) {
            var systemPrompt = ChatSystemPromptBase + "\n\n" + (allowEdits ? ChatEditInstructions : ChatNoEditInstructions) +
                                "\n\n" + SummarizePlan(plan) + "\n\n" + userContext;

            // histórico manda só o texto que o Zelo de fato "disse" (reply já extraído), não o
            // envelope JSON bruto — senão o modelo veria seu próprio JSON como parte da conversa
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
                return new ZeloChatContinuationResult(false, ChatFriendlyErrorMessage, null);
            }

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("API da Anthropic respondeu {StatusCode} para o chat do Zelo.", (int)response.StatusCode);
                return new ZeloChatContinuationResult(false, ChatFriendlyErrorMessage, null);
            }

            // ReadAsStringAsync() decide o charset pelo Content-Type da resposta e, sem um charset
            // explícito, acabou lendo como Latin-1 na prática — acentos em português saíam
            // duplamente corrompidos (achado testando ao vivo). A API da Anthropic sempre responde
            // em UTF-8, então força a decodificação em vez de confiar na detecção automática.
            var jsonBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var json      = Encoding.UTF8.GetString(jsonBytes);
            string? text;
            try {
                using var doc = JsonDocument.Parse(json);
                text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            } catch (Exception ex) {
                _logger.LogError(ex, "Não foi possível ler a resposta da Anthropic para o chat do Zelo.");
                return new ZeloChatContinuationResult(false, ChatFriendlyErrorMessage, null);
            }

            if (string.IsNullOrWhiteSpace(text)) {
                _logger.LogError("Resposta da Anthropic para o chat do Zelo veio sem texto.");
                return new ZeloChatContinuationResult(false, ChatFriendlyErrorMessage, null);
            }

            var stripped = StripJsonFence(text);
            var (reply, proposal) = ParseChatResponse(stripped);

            // chat é mais tolerante que a geração do plano: se o envelope JSON vier malformado,
            // usa o texto cru como resposta em vez de derrubar a conversa inteira por um formato
            // levemente errado — só perde a chance de propor edição nesse turno
            return reply is not null
                ? new ZeloChatContinuationResult(true, reply, proposal)
                : new ZeloChatContinuationResult(true, stripped.Trim(), null);
        }

        // resumo compacto do plano pro prompt do chat — não manda o JSON inteiro pra não estourar o
        // orçamento de tokens à toa. Nutrição vai por refeição (não só os nomes do dia inteiro
        // juntos): edições pontuais ("adiciona proteína no almoço de quarta") precisam saber
        // exatamente o que já tem em CADA refeição, não só no dia.
        private static string SummarizePlan(GeneratedPlan plan) {
            var sb = new StringBuilder();
            sb.Append("PLANO ATUAL DO USUÁRIO\n").Append(plan.Summary).AppendLine();

            sb.AppendLine("Treino:");
            foreach (var day in plan.WorkoutDays) {
                var exercises = day.Exercises.Count == 0
                    ? "descanso"
                    : string.Join(", ", day.Exercises.Select(e => e.ExerciseName));
                var label = day.Label is not null ? $" ({day.Label})" : "";
                sb.Append("- ").Append(day.DayOfWeek).Append(label).Append(": ").AppendLine(exercises);
            }

            sb.AppendLine("Nutrição (por refeição):");
            foreach (var day in plan.NutritionDays) {
                sb.Append("- ").Append(day.DayOfWeek).AppendLine(":");
                foreach (var meal in day.Items.GroupBy(i => i.MealType)) {
                    var items = string.Join(", ", meal.Select(i => $"{i.ItemName} ({i.Quantity})"));
                    sb.Append("  ").Append(meal.Key).Append(": ").AppendLine(items.Length > 0 ? items : "vazio");
                }
            }

            return sb.ToString();
        }

        private const string ChatSystemPromptBase =
            "Você é o Zelo, assistente pessoal dentro do app Pyrra. O usuário tem um plano de Treino " +
            "e Nutrição (resumido abaixo). Responda dúvidas e pedidos de ajuste sobre esse plano de " +
            "forma direta, breve (2-4 frases) e encorajadora.\n\n" +
            "Responda SEMPRE com um objeto JSON válido, sem markdown, sem texto antes ou depois, " +
            "exatamente neste formato:\n" +
            "{ \"reply\": \"sua resposta em texto, 2-4 frases\", \"editProposal\": null }";

        // sessão já Aplicada: o Zelo pode propor uma edição pontual em vez de só responder
        private const string ChatEditInstructions =
            "O plano já foi aplicado. Se o usuário pedir uma mudança pontual (trocar um dia de " +
            "treino inteiro, tirar/adicionar/ajustar um item de UMA refeição), preencha " +
            "\"editProposal\" (em vez de null) neste formato:\n" +
            "{\n" +
            "  \"description\": \"frase curta resumindo a mudança, ex.: 'Trocar treino de Terça para pernas'\",\n" +
            "  \"target\": \"Treino\" ou \"Nutricao\",\n" +
            "  \"dayOfWeek\": um de Segunda, Terca, Quarta, Quinta, Sexta, Sabado, Domingo (sem acento),\n" +
            "  \"label\": rótulo do dia depois da mudança — só quando target=Treino (null se for descanso; null quando target=Nutricao),\n" +
            "  \"exercises\": lista COMPLETA dos exercícios do dia DEPOIS da mudança — só quando target=Treino (null quando target=Nutricao), " +
            "cada item { \"type\": \"Academia\" ou \"Corrida\", \"exerciseName\", \"sets\", \"reps\", \"order\" }. " +
            "Em \"Corrida\", sets e reps são sempre null (nunca string) e exerciseName descreve o treino " +
            "(ex.: \"5km leve\", \"tiros 6x400m\"). Em \"Academia\", sets e reps são sempre números,\n" +
            "  \"mealType\": \"CafeDaManha\", \"Almoco\", \"Lanche\" ou \"Jantar\" — só quando target=Nutricao (null quando target=Treino),\n" +
            "  \"items\": lista COMPLETA dos itens dessa refeição DEPOIS da mudança — só quando target=Nutricao (null quando target=Treino), " +
            "cada item { \"itemName\", \"quantity\" }\n" +
            "}\n" +
            "IMPORTANTE: \"exercises\"/\"items\" é sempre a lista inteira depois da mudança (não só o " +
            "que mudou) — ela substitui tudo daquele dia (treino) ou daquela refeição (nutrição). " +
            "Proponha só UMA edição por vez, sempre sobre um único dia inteiro (treino) ou uma única " +
            "refeição de um único dia (nutrição) — nunca o plano inteiro nem múltiplos dias. Se não " +
            "tiver certeza de qual dia ou refeição o usuário quer mudar, pergunte antes de propor " +
            "(editProposal continua null nesse turno). Não aplique nada sozinho — só proponha; quem " +
            "confirma é o usuário.";

        // sessão ainda não aplicada (preview) — comportamento de sempre, sem propor edição
        private const string ChatNoEditInstructions =
            "Você ainda não pode propor edição nesta conversa — o plano gerado ainda não foi " +
            "aplicado. Se o usuário pedir uma mudança, explique a sugestão em texto e diga que dá " +
            "pra pedir esse tipo de ajuste depois de aplicar o plano. \"editProposal\" deve ser " +
            "sempre null.";

        // o modelo às vezes envolve o JSON em markdown apesar da instrução, ou volta texto puro
        // quando o formato falha — reply nulo sinaliza pro chamador cair no fallback de texto cru
        private static (string? Reply, ZeloEditProposal? Proposal) ParseChatResponse(string json) {
            ChatResponseJson? raw;
            try {
                raw = JsonSerializer.Deserialize<ChatResponseJson>(json, ResponseJsonOptions);
            } catch (JsonException) {
                return (null, null);
            }

            if (raw is null || string.IsNullOrWhiteSpace(raw.Reply)) {
                return (null, null);
            }

            // proposta malformada não derruba a resposta — só vira "sem proposta" nesse turno
            return (raw.Reply.Trim(), ParseEditProposal(raw.EditProposal));
        }

        private static ZeloEditProposal? ParseEditProposal(EditProposalJson? raw) {
            if (raw is null || string.IsNullOrWhiteSpace(raw.Description) || string.IsNullOrWhiteSpace(raw.Target)
                || string.IsNullOrWhiteSpace(raw.DayOfWeek)) {
                return null;
            }

            if (!Enum.TryParse<ZeloEditTarget>(raw.Target, out var target)) {
                return null;
            }
            if (!Enum.TryParse<WeekDay>(raw.DayOfWeek, out var dayOfWeek)) {
                return null;
            }

            if (target == ZeloEditTarget.Treino) {
                if (raw.Exercises is null) {
                    return null;
                }

                var exercises = new List<GeneratedWorkoutExercise>();
                foreach (var exercise in raw.Exercises) {
                    if (!Enum.TryParse<WorkoutType>(exercise.Type, out var type) || string.IsNullOrWhiteSpace(exercise.ExerciseName)) {
                        return null;
                    }
                    exercises.Add(new GeneratedWorkoutExercise(type, exercise.ExerciseName.Trim(), exercise.Sets, exercise.Reps, exercise.Order));
                }

                return new ZeloEditProposal(
                    raw.Description.Trim(), target, dayOfWeek,
                    string.IsNullOrWhiteSpace(raw.Label) ? null : raw.Label.Trim(), exercises, null, null);
            }

            if (string.IsNullOrWhiteSpace(raw.MealType) || raw.Items is null) {
                return null;
            }
            if (!Enum.TryParse<MealType>(raw.MealType, out var mealType)) {
                return null;
            }

            var items = new List<GeneratedNutritionItem>();
            foreach (var item in raw.Items) {
                if (string.IsNullOrWhiteSpace(item.ItemName) || string.IsNullOrWhiteSpace(item.Quantity)) {
                    return null;
                }
                items.Add(new GeneratedNutritionItem(mealType, item.ItemName.Trim(), item.Quantity.Trim()));
            }

            return new ZeloEditProposal(raw.Description.Trim(), target, dayOfWeek, null, null, mealType, items);
        }

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
            "nas respostas. Os 7 dias da semana devem aparecer exatamente uma vez em workoutDays e em nutritionDays.\n\n" +
            "Nutrição — por padrão, monte a dieta com alimentos comuns e financeiramente acessíveis no " +
            "Brasil (ex.: arroz, feijão, ovos, frango, carne moída, banana, aveia, batata, pão, leite, " +
            "queijo comum, verduras e legumes da estação). Evite itens caros ou pouco usuais no dia a " +
            "dia (ex.: salmão, pasta de amendoim, quinoa, castanhas importadas, iogurte grego, whey " +
            "protein) A MENOS QUE a resposta sobre orçamento diga que o usuário não se preocupa com " +
            "custo, ou uma restrição alimentar exija um substituto mais caro. Isso vale mesmo sem " +
            "pergunta explícita de orçamento nas respostas — a acessibilidade é o padrão.";

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

        // formato bruto da resposta de cada turno do chat livre, antes de validar contra os enums do domínio
        private sealed class ChatResponseJson {
            [JsonPropertyName("reply")]
            public string? Reply { get; set; }

            [JsonPropertyName("editProposal")]
            public EditProposalJson? EditProposal { get; set; }
        }

        // "exercises"/"items" reaproveitam WorkoutExerciseJson/NutritionItemJson (mesmo formato dos itens usados na geração do plano)
        private sealed class EditProposalJson {
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("target")]
            public string? Target { get; set; }

            [JsonPropertyName("dayOfWeek")]
            public string? DayOfWeek { get; set; }

            [JsonPropertyName("label")]
            public string? Label { get; set; }

            [JsonPropertyName("exercises")]
            public List<WorkoutExerciseJson>? Exercises { get; set; }

            [JsonPropertyName("mealType")]
            public string? MealType { get; set; }

            [JsonPropertyName("items")]
            public List<NutritionItemJson>? Items { get; set; }
        }
    }
}
