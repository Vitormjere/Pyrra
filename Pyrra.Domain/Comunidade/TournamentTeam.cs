using System;

namespace Pyrra.Domain.Comunidade {
    /// <summary>
    /// Um time participando (ou solicitando participar) de um torneio. Sem FK para
    /// Tournament/Team, mesma convenção do projeto. A existência da linha já é o pedido — igual a
    /// TournamentRequest, cada tentativa nova gera uma linha (não reaproveita uma Recusada), já
    /// que times podem tentar entrar em torneios diferentes ao longo do tempo.
    ///
    /// Score é a pontuação DENTRO deste torneio, separada de Team.TotalPoints — começa em 0 e só
    /// passa a ser somada quando uma submissão de desafio é aprovada com o time Aprovado aqui
    /// (próxima etapa). Um time só pode ter UMA linha ativa (Pendente ou Aprovado) por vez, em
    /// qualquer torneio — "um torneio por vez".
    /// </summary>
    public class TournamentTeam {
        public Guid Id { get; set; }
        public Guid TournamentId { get; set; }
        public Guid TeamId { get; set; }
        public TournamentTeamStatus Status { get; set; } = TournamentTeamStatus.Pendente;
        public int Score { get; set; }
        public DateTime RequestedAt { get; set; }

        // Nulo enquanto Pendente. Preenchido ao aprovar/recusar.
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByUserId { get; set; }
    }

    public enum TournamentTeamStatus {
        Pendente,
        Aprovado,
        Recusado
    }
}
