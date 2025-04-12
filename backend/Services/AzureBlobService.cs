using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace backend.Services
{
    public class AzureBlobService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobService(IConfiguration config)
        {
            var connectionString = config["AzureSettings:ConnectionString"];
            var containerName = config["AzureSettings:ContainerName"];
            _containerClient = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<string> UploadFileAsync(string blobName, byte[] fileBytes)
        {
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);

            using (var stream = new MemoryStream(fileBytes))
            {
                var response = await blobClient.UploadAsync(stream);
            }

            return blobClient.Uri.ToString();

        }

        public async Task<byte[]> DownloadFileAsync(string blobUrl)
        {
            var blobClient = new BlobClient(new Uri(blobUrl));

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);

            return memoryStream.ToArray();
        }

        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            var blobUri = new Uri(blobUrl);
            string absolutePath = blobUri.AbsolutePath;
            if (absolutePath.StartsWith("/"))
            {
                absolutePath = absolutePath.Substring(1);
            }

            string decodedBlobName = Uri.UnescapeDataString(absolutePath);
            var segments = decodedBlobName.Split(new[] { '/' }, 2);
            string blobName =  segments.Length > 1 ? segments[1] : segments[0];

            var blobClient = _containerClient.GetBlobClient(blobName);

            try
            {
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error deleting blob: {ex.Message}");
                return false;
            }
        }

    }
}