using System.Collections.Generic;
using System.Linq;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Zelo {
    // pergunta do formulário guiado — Options nulo significa texto livre
    public record ZeloPlanQuestionInfo(string Key, string Text, IReadOnlyList<string>? Options);

    // Perguntas fixas (sempre nesta ordem) + regras de pergunta dinâmica condicionadas às respostas
    // já dadas. Determinístico de propósito: nenhuma chamada à IA decide o que perguntar, só o
    // conteúdo final do plano depende do modelo.
    public static class ZeloPlanQuestionFlow {
        public const string KeyObjetivo          = "objetivo";
        public const string KeyRestricoes        = "restricoes";
        public const string KeyOrcamento         = "orcamento";
        public const string KeyEquipamento       = "equipamento";
        public const string KeyDias              = "dias";
        public const string KeyCardioMusculacao  = "cardio_musculacao";
        public const string KeyTempoTreino       = "tempo_treino";
        public const string KeyEspacoCasa        = "espaco_casa";
        public const string KeyRefeicoesDia      = "refeicoes_dia";

        private const string ObjetivoEmagrecimento = "Emagrecimento";
        private const string ObjetivoGanhoMassa     = "Ganho de massa muscular";
        private const string EquipamentoNenhum      = "Nenhum (casa)";

        private static readonly IReadOnlyList<string> ObjetivoOptions = new[] {
            ObjetivoEmagrecimento, ObjetivoGanhoMassa, "Condicionamento físico", "Manter a forma"
        };

        private static readonly IReadOnlyList<string> EquipamentoOptions = new[] {
            EquipamentoNenhum, "Halteres ou elásticos em casa", "Academia completa"
        };

        // feedback de usuários reais: a dieta vinha com itens caros/pouco usuais (salmão, pasta de
        // amendoim) que não refletiam o orçamento da maioria — essa resposta chega ao modelo junto
        // das outras (ver BuildUserContent) e o SystemPrompt já assume "econômica" como padrão
        private static readonly IReadOnlyList<string> OrcamentoOptions = new[] {
            "Dieta mais econômica", "Sem preocupação com custo"
        };

        private static readonly IReadOnlyList<string> DiasOptions =
            new[] { "2-3 dias", "4-5 dias", "6+ dias" };

        private static readonly IReadOnlyList<string> CardioMusculacaoOptions =
            new[] { "Mais cardio", "Equilibrado", "Mais musculação" };

        private static readonly IReadOnlyList<string> TempoTreinoOptions =
            new[] { "Menos de 6 meses", "6 meses a 2 anos", "Mais de 2 anos" };

        private static readonly IReadOnlyList<string> EspacoCasaOptions =
            new[] { "Sim", "Não, espaço limitado" };

        private static readonly IReadOnlyList<string> RefeicoesDiaOptions =
            new[] { "3 refeições", "4 refeições", "5-6 refeições pequenas" };

        // ordem fixa das cinco primeiras perguntas — orçamento entra sempre (não é condicional),
        // já que a Nutrição é gerada pra todo mundo independente do objetivo
        private static readonly IReadOnlyList<ZeloPlanQuestionInfo> FixedQuestions = new[] {
            new ZeloPlanQuestionInfo(KeyObjetivo, "Qual é o seu objetivo principal?", ObjetivoOptions),
            new ZeloPlanQuestionInfo(KeyRestricoes, "Alguma restrição física ou alimentar? (lesão, alergia, vegetariano...)", null),
            new ZeloPlanQuestionInfo(KeyOrcamento, "Como você prefere que a dieta seja em relação a custo?", OrcamentoOptions),
            new ZeloPlanQuestionInfo(KeyEquipamento, "Que equipamento você tem disponível?", EquipamentoOptions),
            new ZeloPlanQuestionInfo(KeyDias, "Quantos dias por semana você consegue treinar?", DiasOptions),
        };

        // devolve a próxima pergunta a fazer, ou null quando o formulário está completo
        public static ZeloPlanQuestionInfo? GetNextQuestion(IReadOnlyList<ZeloPlanAnswer> answered) {
            var answeredKeys = answered.Select(a => a.Key).ToHashSet();

            foreach (var question in FixedQuestions) {
                if (!answeredKeys.Contains(question.Key)) {
                    return question;
                }
            }

            var byKey = answered.ToDictionary(a => a.Key, a => a.Answer);
            byKey.TryGetValue(KeyObjetivo, out var objetivo);
            byKey.TryGetValue(KeyEquipamento, out var equipamento);

            if (objetivo == ObjetivoEmagrecimento && !answeredKeys.Contains(KeyCardioMusculacao)) {
                return new ZeloPlanQuestionInfo(KeyCardioMusculacao, "Prefere priorizar cardio ou musculação?", CardioMusculacaoOptions);
            }

            if (objetivo == ObjetivoGanhoMassa && !answeredKeys.Contains(KeyTempoTreino)) {
                return new ZeloPlanQuestionInfo(KeyTempoTreino, "Há quanto tempo você treina?", TempoTreinoOptions);
            }

            if (equipamento == EquipamentoNenhum && !answeredKeys.Contains(KeyEspacoCasa)) {
                return new ZeloPlanQuestionInfo(KeyEspacoCasa, "Tem espaço em casa pra se movimentar bastante (pular, correr no lugar)?", EspacoCasaOptions);
            }

            if ((objetivo == ObjetivoEmagrecimento || objetivo == ObjetivoGanhoMassa) && !answeredKeys.Contains(KeyRefeicoesDia)) {
                return new ZeloPlanQuestionInfo(KeyRefeicoesDia, "Quantas refeições por dia você prefere fazer?", RefeicoesDiaOptions);
            }

            return null;
        }
    }
}
