using System;

namespace Pyrra.Domain.Nutricao {
    public class NutritionPlanSeedLog {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // data no fuso do usuário 
        public DateOnly Date { get; set; }

        public DateTime SeededAt { get; set; }
    }
}
