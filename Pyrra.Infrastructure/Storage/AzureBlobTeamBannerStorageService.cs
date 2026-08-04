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

        private static string BlobName(Guid teamId) => teamId.ToString("N");

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
