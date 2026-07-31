using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // Responsável pelo armazenamento das fotos das submissões de desafios
    public interface IChallengeSubmissionStorageService {
        // Envia a foto da submissão para o armazenamento
        Task<string> UploadAsync(Guid submissionId, Stream content, string contentType, CancellationToken cancellationToken = default);

        // Recupera a foto da submissão armazenada
        Task<(Stream Content, string ContentType)> DownloadAsync(Guid submissionId, CancellationToken cancellationToken = default);
    }
}