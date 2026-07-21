using System;

namespace Pyrra.Domain.Focos {
    // Marco atingido, guardado até o frontend confirmar que exibiu a celebração. Vários podem
    // estar pendentes ao mesmo tempo: quem fica dias sem abrir o app pode cruzar mais de um no
    // mesmo acerto.
    public class PendingMilestone {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int Milestone { get; set; }
        public decimal AveragePercentage { get; set; }
        public DateOnly ReachedDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Null enquanto não exibido. O registro é mantido após a confirmação em vez de apagado:
        // a média foi calculada sobre uma janela que um reset de streak torna irreconstruível,
        // então apagar destruiria a única evidência de que o marco existiu.
        public DateTime? AcknowledgedAt { get; set; }
    }
}
