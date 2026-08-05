using System;

namespace Pyrra.Domain.Comunidade {
    // grupo criado por um usuário (o dono) pra reunir amigos e outras pessoas convidadas
    public class Team {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }

        // definido pelo criador, validado só como número positivo (sem mínimo/máximo do sistema)
        public int MemberLimit { get; set; }

        // token estável do link de convite do time 
        public string InviteToken { get; set; } = string.Empty;

        // placeholder pra pontuação coletiva do time, que ainda depende do sistema de pontos por desafio — começa em 0 e nada escreve aqui por enquanto
        public int TotalPoints { get; set; }

        // privado (padrão) só entra por convite direto ou link
        public TeamVisibility Visibility { get; set; } = TeamVisibility.Privado;

        // cor/tema do banner do card 
        public TeamBannerTheme BannerTheme { get; set; } = TeamBannerTheme.Verde;
        public string? BannerImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum TeamVisibility {
        Privado,
        Publico
    }

    public enum TeamBannerTheme {
        Verde,
        Azul,
        Roxo,
        Laranja,
        Vermelho,
        Dourado
    }
}
