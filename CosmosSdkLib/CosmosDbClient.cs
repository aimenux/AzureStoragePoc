using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contracts.Ports.Configuration;
using Contracts.Ports.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace CosmosSdkLib
{
    public sealed class CosmosDbClient<TDocument> : ICosmosDbClient<TDocument>, IDisposable where TDocument : ICosmosDbDocument
    {
        private readonly CosmosClient _client;
        private readonly Container _container;

        public CosmosDbClient(ICosmosSettings settings, string containerName) : this(settings.Url, settings.Key, settings.DatabaseName, containerName)
        {
        }

        public CosmosDbClient(
            string endpointUrl,
            string authKey,
            string databaseName,
            string containerName)
        {
            _client = new CosmosClient(endpointUrl, authKey);
            _container = _client.GetContainer(databaseName, containerName);
            _container.ReadContainerAsync().GetAwaiter().GetResult();
        }

        public async Task<int> ReadThroughputAsync()
        {
            var throughput = await _container.ReadThroughputAsync();
            return throughput ?? throw new Exception("No throughput provisioned");
        }

        public async Task<ICosmosDbResponse<TDocument>> InsertAsync(ICosmosDbRequest<TDocument> request)
        {
            var document = request.Document;
            var partitionKey = new PartitionKey(document.PartitionKey);
            var itemResponse = await _container.CreateItemAsync(document, partitionKey);
            return new CosmosDbResponse<TDocument>(itemResponse.RequestCharge);
        }

        public Task<ICosmosDbResponse<TDocument>> GetAsync(ICosmosDbRequest<TDocument> request)
        {
            return !string.IsNullOrWhiteSpace(request.Query) ? GetByQueryAsync(request) : GetByItemAsync(request);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        private async Task<ICosmosDbResponse<TDocument>> GetByQueryAsync(ICosmosDbRequest request)
        {
            double requestUnits = 0;
            var documents = new List<TDocument>();

            var iterator = _container.GetItemQueryIterator<TDocument>(request.Query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response.Resource);
                requestUnits += response.RequestCharge;
            }

            return new CosmosDbResponse<TDocument>(requestUnits, documents.ToArray());
        }

        private async Task<ICosmosDbResponse<TDocument>> GetByItemAsync(ICosmosDbRequest<TDocument> request)
        {
            var id = request.Document.Id;
            var partitionKey = new PartitionKey(request.Document.PartitionKey);
            var response = await _container.ReadItemAsync<TDocument>(id, partitionKey);
            return new CosmosDbResponse<TDocument>(response.RequestCharge, response.Resource);
        }
    }

    public sealed class CosmosDbClient : ICosmosDbClient, IDisposable
    {
        private readonly CosmosClient _client;
        private readonly Container _container;
        private readonly Database _database;

        public CosmosDbClient(ICosmosSettings settings)
        {
            _client = new CosmosClient(settings.Url, settings.Key);
            _database = _client.GetDatabase(settings.DatabaseName);
        }

        public CosmosDbClient(
            string endpointUrl,
            string authKey,
            string databaseName,
            string containerName = null)
        {
            _client = new CosmosClient(endpointUrl, authKey);
            _container = _client.GetContainer(databaseName, containerName);
        }

        public async Task<int> ReadThroughputAsync()
        {
            var throughput = await _database.ReadThroughputAsync();
            return throughput ?? throw new Exception("No throughput provisioned");
        }

        public async Task<ICosmosDbResponse> InsertAsync(ICosmosDbRequest request)
        {
            var itemResponse = await _container.CreateItemAsync<dynamic>(request.Document);
            return new CosmosDbResponse(itemResponse.RequestCharge);
        }

        public async Task<ICosmosDbResponse> GetAsync(ICosmosDbRequest request)
        {
            double requestUnits = 0;
            var documents = new List<dynamic>();

            var iterator = _container.GetItemQueryIterator<dynamic>(request.Query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                documents.AddRange(response.Resource);
                requestUnits += response.RequestCharge;
            }

            return new CosmosDbResponse(requestUnits, documents);
        }


        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
