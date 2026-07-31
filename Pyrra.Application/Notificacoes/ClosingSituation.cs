namespace Pyrra.Application.Notificacoes {
    // define a proximidade da meta com base no DailyScore atual
    public enum ClosingSituation {
        // sem focos ativos, não há metas para concluir
        SemFocos,

        // Tem focos, mas nenhum concluído ainda hoje
        Nada,

        // Fez algo, mas abaixo de 50%
        Longe,

        // faixa entre 50% e a meta sem incluir a meta
        Perto,

        // Meta batida (>= 70%)
        MetaBatida
    }
}
