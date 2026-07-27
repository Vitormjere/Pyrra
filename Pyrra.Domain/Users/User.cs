using System;

namespace Pyrra.Domain.Users {
    public class User {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Identificador público único, escolhido pelo usuário (ex.: "vitorj", exibido como "@vitorj").
        // Nulo enquanto não escolhido: usuários criados antes da feature de Amigos não têm um, e o
        // gate de username no frontend é o que obriga a preencher no primeiro acesso seguinte.
        // Guardado em minúsculas e sem "@" — o "@" é só apresentação.
        public string? Username { get; set; }

        // Token estável do link de convite pessoal (pyrra/convite/{token}). Gerado sob demanda na
        // primeira vez que o usuário pede o link e nunca muda, para o link compartilhado não expirar.
        // Aleatório em vez de derivado do Id, para o link não expor o UserId.
        public string? InviteToken { get; set; }
        public string Timezone { get; set; } = "America/Sao_Paulo";
        public CommunicationTone CommunicationTone { get; set; }
        public TimeOnly EveningNotificationTime { get; set; }
        public UserPlan Plan { get; set; } = UserPlan.Free;

        // Quando o usuário concluiu (ou pulou) o onboarding de primeiro acesso. Nulo = ainda não
        // passou por ele, e é isso que o frontend usa para decidir mostrar o fluxo uma única vez.
        public DateTime? OnboardingCompletedAt { get; set; }

        // Soft delete: nulo = conta ativa. Marcado (nunca removido de fato) quando o usuário exclui
        // a própria conta. O UserRepository filtra DeletedAt != null em toda consulta, então uma
        // conta excluída simplesmente some de login, busca, amigos e qualquer endpoint que carregue
        // o usuário pelo id do token — sem precisar repetir a checagem em cada serviço. Email e
        // Username NÃO são liberados: ficam reservados para essa conta, o que também é o que torna
        // a restauração manual (suporte zerando este campo) trivial.
        public DateTime? DeletedAt { get; set; }

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
}