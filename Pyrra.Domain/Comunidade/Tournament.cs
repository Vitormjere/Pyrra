using System;

namespace Pyrra.Domain.Comunidade {
    /// <summary>
    /// Agrupa vários Times pra competir entre si. Sem FK para Times (relação vive em
    /// TournamentTeam, próxima etapa) — mesma convenção do projeto. Reaproveita TeamBannerTheme
    /// (não duplica um enum igual) porque Tournament vive no mesmo bounded context de Team, ao
    /// contrário de ChallengeCategoryColor, que é de um contexto separado (Desafios).
    /// </summary>
    public class Tournament {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }

        // Token estável do link de convite do torneio — mesmo padrão de Team.InviteToken, gerado
        // na criação (direta por admin ou na aprovação de uma TournamentRequest).
        public string InviteToken { get; set; } = string.Empty;

        public TeamBannerTheme BannerTheme { get; set; } = TeamBannerTheme.Verde;

        // Nulo = usa BannerTheme. Preenchido = imagem customizada, prioridade na exibição — mesmo
        // critério de Team.BannerImageUrl.
        public string? BannerImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
