using System;

namespace Pyrra.Domain.Comunidade {
    // agrupa vários times pra competir entre si 
    public class Tournament {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }

        // token estável do link de convite 
        public string InviteToken { get; set; } = string.Empty;

        public TeamBannerTheme BannerTheme { get; set; } = TeamBannerTheme.Verde;

        // nulo usa BannerTheme, preenchido é imagem customizada com prioridade na exibição
        public string? BannerImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
