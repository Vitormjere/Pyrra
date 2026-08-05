using System;

namespace Pyrra.Domain.Financas {
    // lançamento manual
    public class FinanceEntry {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }

        // sempre positivo
        public decimal Amount { get; set; }

        public FinanceEntryType Type { get; set; }

        // data no fuso do usuário, mesmo critério dos outros módulos
        public DateOnly Date { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // totais de um recorte de lançamentos 
    public record FinanceTotals(decimal TotalIn, decimal TotalOut) {
        public decimal Balance => TotalIn - TotalOut;
    }
}
