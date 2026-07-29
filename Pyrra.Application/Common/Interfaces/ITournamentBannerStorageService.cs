using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // Abstrai o Blob Storage por trás do upload de banner de torneio — mesmo espírito de
    // ITeamBannerStorageService (container próprio "tournament-banners", não reaproveita o de
    // time). Um banner por torneio, reupload sobrescreve.
    public interface ITournamentBannerStorageService {
        Task<string> UploadAsync(Guid tournamentId, Stream content, string contentType, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    }
}
