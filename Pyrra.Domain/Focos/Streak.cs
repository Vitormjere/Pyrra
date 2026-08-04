using System;

namespace Pyrra.Domain.Focos {
    public class Streak {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int CurrentCount { get; set; }
        public int BestCount { get; set; }

        // última data já avaliada no streak 
        public DateOnly LastSettledDate { get; set; }

        // primeiro dia da sequência atual, nulo quando o streak está zerado 
        public DateOnly? StreakStartDate { get; set; }

        // dia do último marco atingido, nulo se a sequência ainda não cruzou nenhum 
        public DateOnly? LastMilestoneDate { get; set; }
    }
}
