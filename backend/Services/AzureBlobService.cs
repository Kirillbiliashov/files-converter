using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            // if (!blobClient.Exists())
            // {
            //     await blobClient.
            // }

            using (var stream = new MemoryStream(fileBytes))
            {
                // Upload the bytes as a blob
                var response = await blobClient.UploadAsync(stream);
            }

            return blobClient.Uri.ToString();

        }

    }
}