using System;

namespace Pyrra.Domain.Nutricao {
    // Um item de uma refeição de um dia. Não há entidade "refeição": a refeição é só o agrupamento
    // dos itens que compartilham Date + MealType, o que evita ter de criar um registro vazio de
    // refeição antes de lançar o primeiro item.
    public class NutritionEntry {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // Data no fuso do usuário, mesmo critério dos outros módulos.
        public DateOnly Date { get; set; }

        public MealType MealType { get; set; }
        public string ItemName { get; set; } = string.Empty;

        // Texto livre ("2 ovos", "1 prato"): sem unidade estruturada e sem cálculo de caloria,
        // por decisão de produto.
        public string Quantity { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
