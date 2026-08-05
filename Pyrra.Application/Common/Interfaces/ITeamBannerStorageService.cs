using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // cuida do armazenamento das imagens de banner dos times
    public interface ITeamBannerStorageService {
        Task<string> UploadAsync(Guid teamId, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid teamId, CancellationToken cancellationToken = default);
    }
}