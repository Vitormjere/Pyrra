using System;

namespace Pyrra.Domain.Zelo {
    // histórico do chat livre pós-formulário, mesmo padrão do ChatMessage
    public class ZeloPlanMessage {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public ZeloPlanMessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // preenchido só em mensagens do Zelo que propõem uma edição pontual no plano já aplicado
        // (Fase 1 de edição via chat) — JSON de ZeloEditProposal, null quando é só uma resposta normal
        public string? EditProposalJson { get; set; }
        public ZeloEditStatus EditStatus { get; set; } = ZeloEditStatus.Nenhuma;
    }

    public enum ZeloPlanMessageRole {
        Usuario,
        Zelo
    }

    public enum ZeloEditStatus {
        Nenhuma,
        Proposta,
        Aplicada,
        Descartada
    }
}
