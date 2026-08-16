using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    // gerencia os dados e ações da conta do usuário
    public interface IUserAccountService {
        Task<User> UpdateNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        // exige a senha atual para trocar o e-mail
        Task<User> ChangeEmailAsync(Guid userId, string newEmail, string currentPassword, CancellationToken cancellationToken = default);

        // exige a senha atual para trocar a senha
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        Task<User> UpdateTimezoneAsync(Guid userId, string timezoneId, CancellationToken cancellationToken = default);

        // atualiza a visibilidade do perfil
        Task<User> UpdateProfileVisibilityAsync(Guid userId, ProfileVisibility visibility, CancellationToken cancellationToken = default);

        // cor de destaque do app (botões, links, gráficos etc. — ver AccentColor)
        Task<User> UpdateAccentColorAsync(Guid userId, AccentColor color, CancellationToken cancellationToken = default);

        // desativa a conta sem remover os dados
        Task DeleteAccountAsync(Guid userId, string currentPassword, CancellationToken cancellationToken = default);

        // mesma validação de tipo/tamanho do banner de time (JPG/PNG/WEBP, até 3MB)
        Task<User> SetProfilePictureAsync(
            Guid userId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default);

        // volta pro fallback de inicial
        Task<User> RemoveProfilePictureAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}