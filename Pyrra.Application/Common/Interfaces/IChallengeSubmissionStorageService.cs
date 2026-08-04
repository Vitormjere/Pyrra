using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // cuida do armazenamento das fotos de submissão de desafio
    public interface IChallengeSubmissionStorageService {
        Task<string> UploadAsync(Guid submissionId, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task<(Stream Content, string ContentType)> DownloadAsync(Guid submissionId, CancellationToken cancellationToken = default);
    }
}