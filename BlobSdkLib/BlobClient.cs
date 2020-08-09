using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Contracts.Ports.BlobStorage;
using Contracts.Ports.Configuration;
using Newtonsoft.Json;

namespace BlobSdkLib
{
    public class BlobClient : IBlobClient
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ConcurrentDictionary<string, BlobContainerClient> _blobContainerClients;

        public BlobClient(IBlobSettings settings)
        {
            _blobServiceClient = new BlobServiceClient(settings.ConnectionString);
            _blobContainerClients = new ConcurrentDictionary<string, BlobContainerClient>();
        }

        public async Task<TBlob> GetBlobAsync<TBlob, TBlobDocument>(string name) 
            where TBlob : class, IBlobModel<TBlobDocument>
            where TBlobDocument : class
        {
            var containerName = BlobContainerAttribute.GetContainerName<TBlobDocument>();
            var blobContainerClient = GetOrCreateBlobContainer(containerName);
            var blobClient = blobContainerClient.GetBlobClient(name);
            var exists = await blobClient.ExistsAsync();
            if (!exists) return default;

            var response = await blobClient.DownloadAsync();
            var download = response.Value;
            var reader = new StreamReader(download.Content);
            var content = await reader.ReadToEndAsync();
            var document = JsonConvert.DeserializeObject<TBlobDocument>(content);
            var metadata = download.Details.Metadata;

            var blobModel = new BlobModel<TBlobDocument>
            {
                Name = name,
                Document = document,
                Metadata = metadata
            };

            return blobModel as TBlob;
        }

        public async Task SaveBlobAsync<TBlob, TBlobDocument>(TBlob blob)
            where TBlob : class, IBlobModel<TBlobDocument>
            where TBlobDocument : class
        {
            var containerName = BlobContainerAttribute.GetContainerName<TBlobDocument>();
            var blobContainerClient = GetOrCreateBlobContainer(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blob.Name);
            var exists = await blobClient.ExistsAsync();
            if (exists)
            {
                throw new ArgumentException($"Blob {blob.Name} already exists!");
            }
            using (var memoryStream = SerializeToStream(blob.Document))
            {
                await blobClient.UploadAsync(memoryStream, metadata: blob.Metadata);
            }
        }

        private BlobContainerClient GetOrCreateBlobContainer(string containerName)
        {
            var lowerContainerName = containerName.ToLower();

            if (_blobContainerClients.TryGetValue(lowerContainerName, out var blobContainerClient))
            {
                return blobContainerClient;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(lowerContainerName);
            if (!containerClient.Exists())
            {
                containerClient = _blobServiceClient.CreateBlobContainer(lowerContainerName);
            }

            _blobContainerClients.TryAdd(lowerContainerName, containerClient);

            return containerClient;
        }

        private static MemoryStream SerializeToStream(object obj)
        {
            var stream = new MemoryStream();
            var json = JsonConvert.SerializeObject(obj);
            var writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
