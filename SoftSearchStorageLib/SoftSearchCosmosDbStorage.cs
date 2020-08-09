using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlobSdkLib;
using Contracts.Extensions;
using Contracts.Ports.BlobStorage;
using Contracts.Ports.Configuration;
using Contracts.Ports.CosmosDb;
using Contracts.Ports.CosmosDb.Documents;
using Contracts.Ports.SearchService;
using CosmosSdkLib;
using Polly;
using SearchSdkLib;
using SoftSearchStorageLib.Documents.Blobs;
using SoftSearchStorageLib.Documents.Cosmos;
using SoftSearchStorageLib.Exceptions;
using SoftSearchStorageLib.SearchIndexes;
using SoftSearchStorageLib.SearchModels;

namespace SoftSearchStorageLib
{
    public class SoftSearchCosmosDbStorage : ICosmosDbStorage
    {
        private readonly IBlobClient _blobClient;
        private readonly ISearchClient<OrderSearchIndex> _searchClient;
        private readonly CosmosDbClient<SoftCosmosDataDocument> _dataCosmosDb;
        private readonly CosmosDbClient<SoftCosmosPayloadDocument> _payloadCosmosDb;

        public SoftSearchCosmosDbStorage(ISettings settings)
        {
            _blobClient = new BlobClient(settings.BlobSettings);
            _searchClient = new SearchClient<OrderSearchIndex>(settings.SearchSettings);
            _dataCosmosDb = new CosmosDbClient<SoftCosmosDataDocument>(settings.CosmosSettings, nameof(Containers.Data));
            _payloadCosmosDb = new CosmosDbClient<SoftCosmosPayloadDocument>(settings.CosmosSettings, nameof(Containers.Payload));
        }

        public async Task<ICosmosDbResponse> GetAsync(string orderId)
        {
            const int maxRetry = 5;
            var orderSearchModels = await Policy.Handle<UnfoundOrderException>()
                .RetryAsync(maxRetry, onRetry: (exception, retryCount) =>
                {
                    ConsoleColor.Red.WriteLine($"Unfound order '{orderId}' Retry '{retryCount}'\n");
                })
                .ExecuteAsync(() => GetOrderSearchModelAsync(orderId));

            var filteredOrderSearchModels = orderSearchModels
                .Where(x => x.OrderId == orderId)
                .ToList();

            var orderSearchModel = filteredOrderSearchModels.SingleOrDefault();
            if (orderSearchModel == null)
            {
                return new CosmosDbResponse(0)
                {
                    DynamicInformations = new Dictionary<string, object>
                    {
                        ["Unfound OrderId in Search"] = orderId
                    }
                };
            }

            var transactionId = orderSearchModel.TransactionId;
            var dataRequest = CosmosDbRequest<SoftCosmosDataDocument>.BuildBasedOnPartitionKeyAndId(transactionId, transactionId);
            var dataResponse = await _dataCosmosDb.GetAsync(dataRequest);
            var dataDocument = dataResponse.Documents.SingleOrDefault();
            if (dataDocument == null)
            {
                return new CosmosDbResponse(0)
                {
                    DynamicInformations = new Dictionary<string, object>
                    {
                        ["Found Id in Search"] = orderId,
                        ["Unfound TransactionId in Cosmos"] = transactionId,
                    }
                };
            }

            var blobName = GenerateTransactionBlobName(transactionId);
            var dataBlob = await _blobClient.GetBlobAsync<BlobModel<TransactionDataBlobDocument>, TransactionDataBlobDocument>(blobName);
            var order = dataBlob.Document
                .Response
                .Orders
                .Single(x => x.OrderId == orderId);

            return new CosmosDbResponse(dataResponse.RequestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataResponse)] = dataResponse,
                    [nameof(orderId)] = orderId,
                    [nameof(transactionId)] = transactionId,
                    ["Found data blob"] = blobName
                }
            };
        }

        public async Task<ICosmosDbResponse> SaveAsync(IPayload payload, IData data)
        {
            var blobName = GenerateTransactionBlobName(data.Request.TransactionId);
            var dataDocument = new SoftCosmosDataDocument(data.Request, data.Response, blobName);
            var payloadDocument = new SoftCosmosPayloadDocument(payload.RequestDto, payload.ResponseDto, blobName);
           
            var dataCosmosDbRequest = new CosmosDbRequest<SoftCosmosDataDocument>(dataDocument);
            var payloadCosmosDbRequest = new CosmosDbRequest<SoftCosmosPayloadDocument>(payloadDocument);

            var dataTask = _dataCosmosDb.InsertAsync(dataCosmosDbRequest);
            var payloadTask = _payloadCosmosDb.InsertAsync(payloadCosmosDbRequest);

            var searchModels = BuildOrderSearchModels(data);
            var searchTask = _searchClient.SaveAsync(searchModels);

            var blobsTask = GetSaveTransactionBlobsTask(payload, data);
                
            await Task.WhenAll(dataTask, payloadTask, searchTask, blobsTask);

            var dataRequestUnits = (await dataTask).RequestUnits;
            var payloadRequestUnits = (await payloadTask).RequestUnits;
            var requestUnits = dataRequestUnits + payloadRequestUnits;

            var blobs = await blobsTask;

            return new CosmosDbResponse(requestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataRequestUnits)] = $"{dataRequestUnits} RU",
                    [nameof(payloadRequestUnits)] = $"{payloadRequestUnits} RU",
                    [nameof(blobs)] = $"{blobs} uploaded"
                }
            };
        }

        private async Task<ICollection<OrderSearchModel>> GetOrderSearchModelAsync(string orderId)
        {
            var query = $"\"{orderId}\"";
            var searchModels = await _searchClient.GetAsync<OrderSearchModel>(query);
            if (searchModels == null || !searchModels.Any())
            {
                throw UnfoundOrderException.OrderIsUnfound(orderId);
            }

            return searchModels;
        }

        private static ICollection<OrderSearchModel> BuildOrderSearchModels(IData data)
        {
            var transactionId = data.Response.TransactionId;
            var transactionDate = data.Response.TransactionDate;
            return data.Response.Orders
                .Select(x => new OrderSearchModel
                {
                    OrderId = x.OrderId,
                    TransactionId = transactionId,
                    TransactionDate = transactionDate
                })
                .ToList();
        }

        private async Task<int> GetSaveTransactionBlobsTask(IPayload payload, IData data)
        {
            var transactionId = data.Request.TransactionId;
            var blobName = GenerateTransactionBlobName(transactionId);
            var metadata = new Dictionary<string, string>
            {
                [nameof(transactionId)] = transactionId
            };

            var dataBlobDocument = new TransactionDataBlobDocument(data.Request, data.Response);
            var dataBlobModel = new BlobModel<TransactionDataBlobDocument>
            {
                Name = blobName,
                Metadata = metadata,
                Document = dataBlobDocument
            };

            var payloadBlobDocument = new TransactionPayloadBlobDocument(payload.RequestDto, payload.ResponseDto);
            var payloadBlobModel = new BlobModel<TransactionPayloadBlobDocument>
            {
                Name = blobName,
                Metadata = metadata,
                Document = payloadBlobDocument,
            };

            var dataBlobTask = _blobClient.SaveBlobAsync<BlobModel<TransactionDataBlobDocument>,TransactionDataBlobDocument>(dataBlobModel);
            var payloadBlobTask = _blobClient.SaveBlobAsync<BlobModel<TransactionPayloadBlobDocument>,TransactionPayloadBlobDocument>(payloadBlobModel);
            var tasks = new[] {dataBlobTask, payloadBlobTask};

            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        private static string GenerateTransactionBlobName(string transactionId) => $"{transactionId}.txt";
    }

    public enum Containers
    {
        None,
        Data,
        Payload
    }
}
