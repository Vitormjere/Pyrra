using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;

namespace Pyrra.Infrastructure.Storage {
    public class AzureBlobChallengeSubmissionStorageService : IChallengeSubmissionStorageService {
        private const string DefaultContainerName = "challenge-submissions";

        private const string NotConfiguredMessage = "Upload de imagem indisponível no momento.";

        private readonly IConfiguration _configuration;

        public AzureBlobChallengeSubmissionStorageService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task<string> UploadAsync(Guid submissionId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(submissionId));

            await blob.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken);

            return blob.Uri.ToString();
        }

        public async Task<(Stream Content, string ContentType)> DownloadAsync(Guid submissionId, CancellationToken cancellationToken = default) {
            var container = GetContainerClient();
            var blob = container.GetBlobClient(BlobName(submissionId));

            try {
                var result = await blob.DownloadContentAsync(cancellationToken);
                return (result.Value.Content.ToStream(), result.Value.Details.ContentType);
            } catch (RequestFailedException ex) when (ex.Status == 404) {
                throw new NotFoundException("Foto da submissão não encontrada.");
            }
        }
        private static string BlobName(Guid submissionId) => submissionId.ToString("N");

        private BlobContainerClient GetContainerClient() {
            var connectionString = _configuration["BlobStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString)) {
                throw new InvalidChallengeException(NotConfiguredMessage);
            }

            var containerName = _configuration["BlobStorage:ChallengeSubmissionsContainer"];
            if (string.IsNullOrWhiteSpace(containerName)) {
                containerName = DefaultContainerName;
            }

            var serviceClient = new BlobServiceClient(connectionString);
            return serviceClient.GetBlobContainerClient(containerName);
        }
    }
}
