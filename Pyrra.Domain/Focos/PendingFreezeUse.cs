using System;

namespace Pyrra.Domain.Focos {
    // dia perdoado por um freeze durante o acerto do streak, guardado até o frontend confirmar que exibiu o aviso — mesmo padrão do PendingMilestone, vários podem ficar pendentes ao mesmo tempo
    public class PendingFreezeUse {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // o dia que teria quebrado a sequência e foi perdoado pelo freeze
        public DateOnly Date { get; set; }

        public DateTime CreatedAt { get; set; }

        // nulo enquanto não exibido 
        public DateTime? AcknowledgedAt { get; set; }
    }
}
