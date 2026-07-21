using System;

namespace Pyrra.Domain.Financas {
    // Lançamento manual. O saldo NÃO é campo: é sempre derivado da soma de entradas menos saídas,
    // então não existe estado de saldo para ficar dessincronizado dos lançamentos.
    public class FinanceEntry {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }

        // Sempre positivo: o sinal vem do Type, não do valor. Guardar saída como negativo
        // permitiria representar a mesma coisa de duas formas.
        public decimal Amount { get; set; }

        public FinanceEntryType Type { get; set; }

        // Data no fuso do usuário, mesmo critério dos outros módulos.
        public DateOnly Date { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Totais de um recorte de lançamentos. Calculado no banco (SUM), nunca carregando as linhas.
    public record FinanceTotals(decimal TotalIn, decimal TotalOut) {
        public decimal Balance => TotalIn - TotalOut;
    }
}
