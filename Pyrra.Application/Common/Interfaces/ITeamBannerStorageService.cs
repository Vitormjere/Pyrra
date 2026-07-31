using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // Responsável pelo armazenamento das imagens de banner dos times
    public interface ITeamBannerStorageService {
        // Envia a imagem do banner do time para o armazenamento
        Task<string> UploadAsync(Guid teamId, Stream content, string contentType, CancellationToken cancellationToken = default);

        // Remove a imagem do banner do time
        Task DeleteAsync(Guid teamId, CancellationToken cancellationToken = default);
    }
}