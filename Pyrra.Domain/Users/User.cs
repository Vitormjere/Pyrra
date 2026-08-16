using System;

namespace Pyrra.Domain.Users {
    public class User {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;

        // nulo pra conta criada só via Google (sem senha própria) — LoginAsync recusa login por
        // senha nesse caso em vez de tentar verificar contra hash nenhum
        public string? PasswordHash { get; set; }

        public string Name { get; set; } = string.Empty;

        // "sub" do token do Google — nulo até a conta ser criada ou vinculada via login com
        // Google (ver AuthService.LoginWithGoogleAsync). Único quando presente.
        public string? GoogleId { get; set; }

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

        // true por padrão (contas existentes antes dessa coluna existir "ganham" o confirmado,
        // já estavam em uso) — RegisterAsync começa contas novas por e-mail/senha em false
        // explicitamente; contas por Google nascem true (o Google já verificou o e-mail).
        // Não bloqueia uso do app por enquanto, só fica registrado.
        public bool EmailConfirmed { get; set; } = true;

        // nulo fora de uma janela de confirmação pendente — gerado em RegisterAsync e reenviado
        // sob pedido, consumido (e limpo) ao confirmar. Expira em 24h.
        public string?   EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

        // mesmo raciocínio do par acima, pro fluxo de "esqueci minha senha" — expira em 1h
        // (mais curto de propósito: é o token que, se vazado, dá acesso total à conta)
        public string?   PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }
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