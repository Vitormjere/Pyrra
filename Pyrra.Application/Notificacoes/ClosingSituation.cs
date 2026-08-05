namespace Pyrra.Application.Notificacoes {
    // define a proximidade da meta com base no DailyScore atual
    public enum ClosingSituation {
        // sem focos ativos, não há metas para concluir
        SemFocos,

        // tem focos, mas nenhum concluído ainda hoje
        Nada,

        // fez algo, mas abaixo de 50%
        Longe,

        // faixa entre 50% e a meta sem incluir a meta
        Perto,

        // meta batida (>= 70%)
        MetaBatida
    }
}
