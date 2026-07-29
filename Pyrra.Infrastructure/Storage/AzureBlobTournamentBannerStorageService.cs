using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;

namespace Pyrra.Infrastructure.Storage {
    // Implementação do upload de banner de torneio via Azure Blob Storage — mesmo desenho de
    // AzureBlobTeamBannerStorageService, container próprio "tournament-banners" (não reaproveita
    // o de time). Público (Blob), igual ao de time: banner não é sensível como prova de desafio.
    public class AzureBlobTournamentBannerStorageService : ITournamentBannerStorageService {
        private const string DefaultContainerName = "tournament-banners";

        private const string NotConfiguredMessage = "Upload de imagem indisponível no momento.";

        private readonly IConfiguration _configuration;

        public AzureBlobTournamentBannerStorageService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task<string> UploadAsync(Guid tournamentId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(tournamentId));

            await blob.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken);

            return blob.Uri.ToString();
        }

        public async Task DeleteAsync(Guid tournamentId, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(tournamentId));
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        private static string BlobName(Guid tournamentId) => tournamentId.ToString("N");

        // Não chama CreateIfNotExistsAsync — mesmo critério do banner de time. O container é
        // criado uma vez (via SDK, com autorização) antes deste serviço funcionar de verdade.
        private BlobContainerClient GetContainerClient() {
            var connectionString = _configuration["BlobStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString)) {
                throw new InvalidTournamentException(NotConfiguredMessage);
            }

            var containerName = _configuration["BlobStorage:TournamentBannersContainer"];
            if (string.IsNullOrWhiteSpace(containerName)) {
                containerName = DefaultContainerName;
            }

            var serviceClient = new BlobServiceClient(connectionString);
            return serviceClient.GetBlobContainerClient(containerName);
        }
    }
}
