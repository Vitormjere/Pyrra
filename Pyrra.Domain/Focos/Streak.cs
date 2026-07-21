using System;

namespace Pyrra.Domain.Focos {
    public class Streak {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int CurrentCount { get; set; }
        public int BestCount { get; set; }

        // Última data já avaliada e incorporada ao streak. Sempre <= ontem: o dia corrente só é
        // avaliado depois que vira passado.
        public DateOnly LastSettledDate { get; set; }

        // Primeiro dia da sequência atual. Null quando o streak está zerado.
        // Serve de limite inicial da média do primeiro marco.
        public DateOnly? StreakStartDate { get; set; }

        // Dia em que o último marco foi atingido. Null se a sequência atual ainda não cruzou
        // nenhum. Sem isso não dá para delimitar a janela da média de um marco cruzado em outra
        // chamada — o "dia seguinte ao marco anterior" seria irrecuperável.
        public DateOnly? LastMilestoneDate { get; set; }
    }
}
