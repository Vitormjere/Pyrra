namespace Pyrra.Application.Notificacoes {
    // Quão perto da meta o usuário está HOJE, derivado do DailyScore ao vivo. Cada faixa casa
    // com um tom de mensagem diferente. A régua da meta (70%) é a mesma do DailyScoreCalculator —
    // aqui só se lê o GoalMet que ele já computou, sem repetir o número.
    public enum ClosingSituation {
        // Nenhum foco ativo cadastrado: não há o que concluir, então não é "não fez nada".
        SemFocos,

        // Tem focos, mas nenhum concluído ainda hoje.
        Nada,

        // Fez algo, mas abaixo de 50%.
        Longe,

        // Entre 50% (inclusive) e a meta (70%, exclusive).
        Perto,

        // Meta batida (>= 70%).
        MetaBatida
    }
}
