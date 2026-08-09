using System;

namespace Pyrra.Domain.Zelo {
    // uma linha por pergunta respondida no formulário guiado (fixas + dinâmicas), na ordem em que apareceram
    public class ZeloPlanAnswer {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
