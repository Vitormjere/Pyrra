using System;

namespace Pyrra.Domain.Zelo {
    // Uma linha por usuário por dia: o contador de perguntas feitas ao Zelo naquele dia. É a base
    // do rate limit diário — o índice único (UserId, Date) garante no banco a semântica de upsert
    // do repositório, mesmo critério do DailyScore.
    public class ZeloQueryLog {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // Data no fuso do usuário, mesmo critério dos outros módulos: o limite vira à meia-noite
        // local, não em UTC.
        public DateOnly Date { get; set; }

        public int Count { get; set; }
    }
}
