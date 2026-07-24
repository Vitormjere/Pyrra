using System;

namespace Pyrra.Domain.Focos {
    // Dia perdoado por um freeze durante o acerto do streak, guardado até o frontend confirmar
    // que exibiu o aviso. Mesmo padrão do PendingMilestone: vários podem ficar pendentes ao
    // mesmo tempo quando alguém some por dias e volta, cruzando mais de um dia perdoado no
    // mesmo acerto.
    public class PendingFreezeUse {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // O dia que teria quebrado a sequência e foi perdoado pelo freeze.
        public DateOnly Date { get; set; }

        public DateTime CreatedAt { get; set; }

        // Null enquanto não exibido. Mantido após a confirmação, como o PendingMilestone —
        // é o registro histórico de que o freeze foi gasto naquele dia.
        public DateTime? AcknowledgedAt { get; set; }
    }
}
