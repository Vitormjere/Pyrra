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
    // Implementação do upload de banner de time via Azure Blob Storage. O BlobServiceClient é
    // montado SOB DEMANDA dentro de cada método, não no construtor: se a connection string ainda
    // não estiver configurada, só o upload/remoção falha (com mensagem amigável) — os outros
    // endpoints de Times (que também dependem de ITeamService) continuam funcionando normalmente.
    public class AzureBlobTeamBannerStorageService : ITeamBannerStorageService {
        private const string DefaultContainerName = "team-banners";

        private const string NotConfiguredMessage = "Upload de imagem indisponível no momento.";

        private readonly IConfiguration _configuration;

        public AzureBlobTeamBannerStorageService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task<string> UploadAsync(Guid teamId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(teamId));

            await blob.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken);

            return blob.Uri.ToString();
        }

        public async Task DeleteAsync(Guid teamId, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(teamId));
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        // Sem extensão de propósito: o Content-Type vai nos metadados do blob (ContentType acima),
        // então o navegador renderiza certo mesmo sem extensão no nome — e um reupload do mesmo
        // time sobrescreve o blob anterior automaticamente, sem deixar nada órfão mesmo que o
        // formato mude entre um upload e outro.
        private static string BlobName(Guid teamId) => teamId.ToString("N");

        // Não chama CreateIfNotExistsAsync: o container é criado manualmente no Portal Azure (fora
        // do código), o que evita exigir permissão de gestão de containers além de leitura/escrita
        // de blob.
        private BlobContainerClient GetContainerClient() {
            var connectionString = _configuration["BlobStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString)) {
                throw new InvalidTeamException(NotConfiguredMessage);
            }

            var containerName = _configuration["BlobStorage:TeamBannersContainer"];
            if (string.IsNullOrWhiteSpace(containerName)) {
                containerName = DefaultContainerName;
            }

            var serviceClient = new BlobServiceClient(connectionString);
            return serviceClient.GetBlobContainerClient(containerName);
        }
    }
}
