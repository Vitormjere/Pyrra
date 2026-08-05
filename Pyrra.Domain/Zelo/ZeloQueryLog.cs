using System;

namespace Pyrra.Domain.Zelo {
    // uma linha por usuário por dia: o contador de perguntas feitas ao Zelo naquele dia
    public class ZeloQueryLog {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // data no fuso do usuário, mesmo critério dos outros módulos: o limite vira à meia-noite local, não em UTC
        public DateOnly Date { get; set; }

        public int Count { get; set; }
    }
}
