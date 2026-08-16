using System;

namespace Pyrra.Domain.Users {
    public class User {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // nulo até o usuário escolher um (contas antigas, principalmente)
        public string? Username { get; set; }

        // nulo até o usuário enviar uma foto — fallback é o círculo com a inicial do nome (ver Avatar.tsx)
        public string? ProfilePictureUrl { get; set; }

        // gerado sob demanda no primeiro pedido de link e nunca muda, senão o compartilhado quebra
        public string? InviteToken { get; set; }
        public string Timezone { get; set; } = "America/Sao_Paulo";
        public CommunicationTone CommunicationTone { get; set; }
        public TimeOnly EveningNotificationTime { get; set; }
        public UserPlan Plan { get; set; } = UserPlan.Free;

        // cor de destaque escolhida pelo usuário (botões, links, ícones ativos, gráficos, splash,
        // badges — tudo que hoje usa --color-brand-green no frontend). Verde é o padrão/valor atual.
        public AccentColor AccentColor { get; set; } = AccentColor.Verde;

        // total acumulado, recompensa por conquistas desbloqueadas
        public int Xp { get; set; }

        // público por padrão 
        public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Publico;

        // nulo enquanto não passou pelo onboarding 
        public DateTime? OnboardingCompletedAt { get; set; }

        // soft delete — o repositório já filtra isso em toda consulta, então a conta some de login, busca e afins sem precisar repetir a checagem em cada serviço
        public DateTime? DeletedAt { get; set; }

        // true só é setado por AdminUserService.CreateAdminAccountAsync, que exige que quem chama
        // já seja admin (EnsureAdminAsync) — fora desse caminho, só muda por migration ou SQL direto
        public bool IsAdmin { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // tentativas de login falhas SEGUIDAS pra essa conta (qualquer IP) — reseta a 0 em
        // todo login bem-sucedido; ao atingir o limite, LockedUntil é setado e o contador
        // volta a 0 (a contagem seguinte, após o bloqueio expirar, começa do zero de novo)
        public int FailedLoginAttempts { get; set; }

        // nulo enquanto a conta não está bloqueada; login (mesmo com senha certa) é recusado
        // enquanto LockedUntil > agora
        public DateTime? LockedUntil { get; set; }
    }

    public enum CommunicationTone {
        Direto,
        Acolhedor,
        Desafiador
    }

    public enum UserPlan {
        Free,
        Premium
    }

    public enum ProfileVisibility {
        Publico,
        SomenteAmigos
    }

    // Verde primeiro (= 0) de propósito: é o valor padrão de contas novas e existentes depois da
    // migration, sem precisar de HasDefaultValue explícito no EF.
    public enum AccentColor {
        Verde,
        Azul,
        Rosa,
        Roxo,
        Vermelho,
        Laranja,
        Amarelo
    }
}