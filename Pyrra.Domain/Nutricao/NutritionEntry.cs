using System;

namespace Pyrra.Domain.Nutricao {
    // um item de uma refeição de um dia 
    public class NutritionEntry {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // data no fuso do usuário, mesmo critério dos outros módulos
        public DateOnly Date { get; set; }

        public MealType MealType { get; set; }
        public string ItemName { get; set; } = string.Empty;

        // texto livre ("2 ovos", "1 prato")
        public string Quantity { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
