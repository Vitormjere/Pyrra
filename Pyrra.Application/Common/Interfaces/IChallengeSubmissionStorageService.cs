using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    // Abstrai o Blob Storage por trás do upload de prova por foto — mesmo espírito de
    // ITeamBannerStorageService. Sem DeleteAsync: diferente do banner (um por time, reupload
    // sobrescreve), cada submissão é uma tentativa nova com uma foto própria, nunca sobrescrita.
    //
    // Container PRIVADO (sem acesso público de blob): a foto nunca é servida por link direto, só
    // pelo endpoint autenticado do backend (TeamChallengeController.GetSubmissionPhoto), que
    // valida dono/membro do time antes de chamar DownloadAsync.
    public interface IChallengeSubmissionStorageService {
        // Nome do blob é determinístico pela submissão (implementação usa submissionId.ToString("N")).
        Task<string> UploadAsync(Guid submissionId, Stream content, string contentType, CancellationToken cancellationToken = default);

        // Bytes e Content-Type do blob — lança NotFoundException se não existir (submissão sem
        // foto correspondente, não deveria acontecer no fluxo normal).
        Task<(Stream Content, string ContentType)> DownloadAsync(Guid submissionId, CancellationToken cancellationToken = default);
    }
}
