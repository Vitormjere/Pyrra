using System;

namespace Pyrra.Domain.Zelo {
    // sessão do Zelo conversacional pra montar Treino e Nutrição juntos — separada do Zelo geral (ZeloQueryLog), expira 24h após criada
    public class ZeloPlanSession {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ZeloPlanSessionStatus Status { get; set; } = ZeloPlanSessionStatus.Coletando;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        // preenchido só quando Status >= PlanoGerado. JSON de propósito: é dado de preview,
        // descartável, nunca editado campo a campo — aplicar copia pra WorkoutPlanDay/Exercise e
        // NutritionPlanItem "de verdade"; normalizar em tabelas só pra esse estágio transitório
        // duplicaria o schema à toa.
        public string? GeneratedPlanJson { get; set; }

        public DateTime? AppliedAt { get; set; }
    }

    public enum ZeloPlanSessionStatus {
        Coletando,
        PlanoGerado,
        Aplicada,
        Descartada
    }
}
