using System;

namespace Pyrra.Domain.Comunidade {
    /// <summary>
    /// Grupo criado por um usuário (o dono) para reunir amigos e outras pessoas convidadas. O dono
    /// NÃO tem uma linha própria em TeamMember — a titularidade vive só em OwnerId, e "quantos
    /// membros" é sempre 1 (o dono) + a contagem de TeamMember. Evita OwnerId e uma linha de
    /// membership do dono divergirem entre si.
    /// </summary>
    public class Team {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }

        // Definido pelo criador, validado só como número positivo (sem mínimo/máximo do sistema).
        public int MemberLimit { get; set; }

        // Token estável do link de convite do time (pyrra/times/convite/{token}). Diferente do
        // InviteToken do User (lazy), este é gerado NA CRIAÇÃO: um time sem link ainda não faz
        // sentido para a tela de detalhes, que já mostra o link de cara.
        public string InviteToken { get; set; } = string.Empty;

        // Placeholder para a pontuação coletiva do time, que ainda depende do sistema de pontos por
        // desafio (próximo item do roadmap). Começa em 0 e nada escreve aqui por enquanto.
        public int TotalPoints { get; set; }

        // Privado (padrão): só entra por convite direto ou link, como sempre funcionou. Público:
        // aparece na aba Explorar e qualquer um entra direto pelo mesmo mecanismo de token — ver
        // TeamService.JoinViaLinkAsync, que não muda em nada entre os dois casos.
        public TeamVisibility Visibility { get; set; } = TeamVisibility.Privado;

        // Cor/tema do banner do card — uma das 6 opções fixas, sempre disponível como alternativa
        // rápida mesmo quando há imagem customizada.
        public TeamBannerTheme BannerTheme { get; set; } = TeamBannerTheme.Verde;

        // Nulo = usa BannerTheme (comportamento original). Preenchido = imagem customizada enviada
        // pelo dono, com prioridade sobre a cor na exibição (ver TeamService.ToSummaryAsync e o
        // componente TeamBanner no frontend, que decidem a prioridade olhando só esse campo).
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
