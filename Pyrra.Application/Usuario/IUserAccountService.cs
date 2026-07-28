using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    // Ações da tela de Configurações que mexem na CONTA em si — nome, e-mail, senha, fuso horário e
    // exclusão — separadas do IUserPreferencesService (tom/horário de notificação, que é preferência
    // de produto) e do IUsernameService (identificador público, com sua própria checagem de
    // disponibilidade). O userId sempre vem do token de quem chama; nenhum método aceita agir em
    // nome de outro usuário.
    public interface IUserAccountService {
        Task<User> UpdateNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        // Exige a senha atual: o e-mail é usado como identificador de login, então um token vazado
        // (sem a senha) não deve bastar para sequestrar a conta trocando o e-mail de recuperação.
        Task<User> ChangeEmailAsync(Guid userId, string newEmail, string currentPassword, CancellationToken cancellationToken = default);

        // Mesma exigência de reautenticação da troca de e-mail.
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        Task<User> UpdateTimezoneAsync(Guid userId, string timezoneId, CancellationToken cancellationToken = default);

        // Quem pode ver o perfil público (Público/SomenteAmigos). Sem validação de formato — é um
        // enum, o model binding do controller já recusa valores fora dele antes de chegar aqui.
        Task<User> UpdateProfileVisibilityAsync(Guid userId, ProfileVisibility visibility, CancellationToken cancellationToken = default);

        // Soft delete: marca DeletedAt e nunca mais que devolve o usuário em nenhuma consulta do
        // UserRepository. Exige a senha atual, mesmo critério das outras ações sensíveis.
        Task DeleteAccountAsync(Guid userId, string currentPassword, CancellationToken cancellationToken = default);
    }
}
