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

        // total acumulado, recompensa por conquistas desbloqueadas
        public int Xp { get; set; }

        // público por padrão 
        public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Publico;

        // nulo enquanto não passou pelo onboarding 
        public DateTime? OnboardingCompletedAt { get; set; }

        // soft delete — o repositório já filtra isso em toda consulta, então a conta some de login, busca e afins sem precisar repetir a checagem em cada serviço
        public DateTime? DeletedAt { get; set; }

        // só muda por migration ou SQL direto, nunca por uma requisição 
        public bool IsAdmin { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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
}