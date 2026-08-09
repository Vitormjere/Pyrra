using System;

namespace Pyrra.Domain.Zelo {
    // uma linha por usuário por dia: contador de interações no Zelo conversacional, separado do ZeloQueryLog do Zelo geral
    public class ZeloPlanQueryLog {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // data no fuso do usuário, mesmo critério do ZeloQueryLog
        public DateOnly Date { get; set; }

        public int Count { get; set; }
    }
}
