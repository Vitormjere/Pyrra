using System;

namespace Pyrra.Domain.Desafios {
    // Placar INDIVIDUAL de um usuário dentro de um time específico — separado do Team.TotalPoints
    // (coletivo, soma de todo mundo) e do TournamentTeam.Score (do time inteiro dentro de um
    // torneio). Uma linha por combinação TeamId+UserId: a mesma pessoa em times diferentes tem
    // linhas (e placares) separados, sem se misturar.
    //
    // O DONO do time também tem uma linha aqui, ao contrário de TeamMember (que não tem linha para
    // o dono) — aqui não há esse caso especial, já que o placar é só "quantos pontos essa pessoa
    // ganhou nesse time", e o dono ganha pontos como qualquer outro membro.
    public class TeamMemberScore {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public int Points { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
