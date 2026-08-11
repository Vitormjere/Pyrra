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
    public class AzureBlobUserProfilePictureStorageService : IUserProfilePictureStorageService {
        private const string DefaultContainerName = "profile-pictures";

        private const string NotConfiguredMessage = "Upload de imagem indisponível no momento.";

        private readonly IConfiguration _configuration;

        public AzureBlobUserProfilePictureStorageService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task<string> UploadAsync(Guid userId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(userId));

            await blob.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken);

            return blob.Uri.ToString();
        }

        public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(userId));
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        private static string BlobName(Guid userId) => userId.ToString("N");

        private BlobContainerClient GetContainerClient() {
            var connectionString = _configuration["BlobStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString)) {
                throw new InvalidAccountException(NotConfiguredMessage);
            }

            var containerName = _configuration["BlobStorage:ProfilePicturesContainer"];
            if (string.IsNullOrWhiteSpace(containerName)) {
                containerName = DefaultContainerName;
            }

            var serviceClient = new BlobServiceClient(connectionString);
            return serviceClient.GetBlobContainerClient(containerName);
        }
    }
}
