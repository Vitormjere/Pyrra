using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // cuida do armazenamento das fotos de perfil dos usuários
    public interface IUserProfilePictureStorageService {
        Task<string> UploadAsync(Guid userId, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
