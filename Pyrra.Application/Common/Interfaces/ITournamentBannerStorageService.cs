using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // Responsável pelo armazenamento das imagens de banner dos torneios
    public interface ITournamentBannerStorageService {
        Task<string> UploadAsync(Guid tournamentId, Stream content, string contentType, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    }
}
