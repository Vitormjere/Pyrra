using System;

namespace Pyrra.Domain.Zelo {
    // histórico do chat livre pós-formulário, mesmo padrão do ChatMessage
    public class ZeloPlanMessage {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public ZeloPlanMessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public enum ZeloPlanMessageRole {
        Usuario,
        Zelo
    }
}
