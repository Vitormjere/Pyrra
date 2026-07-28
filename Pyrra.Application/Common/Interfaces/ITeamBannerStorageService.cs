using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // Abstrai o Blob Storage por trás do upload de banner de time — a Application layer não
    // conhece Azure, só sabe subir/apagar bytes e receber uma URL de volta.
    public interface ITeamBannerStorageService {
        // Nome do blob é determinístico por time (implementação usa teamId.ToString("N")), então
        // um reupload sobrescreve o anterior sem deixar blob órfão.
        Task<string> UploadAsync(Guid teamId, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid teamId, CancellationToken cancellationToken = default);
    }
}
