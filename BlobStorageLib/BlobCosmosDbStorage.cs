using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlobSdkLib;
using BlobStorageLib.Documents.Blobs;
using BlobStorageLib.Documents.Cosmos;
using Contracts.Models;
using Contracts.Ports.BlobStorage;
using Contracts.Ports.Configuration;
using Contracts.Ports.CosmosDb;
using Contracts.Ports.CosmosDb.Documents;
using CosmosSdkLib;

namespace BlobStorageLib
{
    public class BlobCosmosDbStorage : ICosmosDbStorage
    {
        private readonly IBlobClient _blobClient;
        private readonly CosmosDbClient<CosmosDataDocument> _dataCosmosDb;
        private readonly CosmosDbClient<CosmosPayloadDocument> _payloadCosmosDb;

        public BlobCosmosDbStorage(ISettings settings)
        {
            _blobClient = new BlobClient(settings.BlobSettings);
            _dataCosmosDb = new CosmosDbClient<CosmosDataDocument>(settings.CosmosSettings, nameof(Containers.Data));
            _payloadCosmosDb = new CosmosDbClient<CosmosPayloadDocument>(settings.CosmosSettings, nameof(Containers.Payload));
        }

        public async Task<ICosmosDbResponse> GetAsync(string orderId)
        {
            var blobName = GenerateOrderBlobName(orderId);
            var blob = await _blobClient.GetBlobAsync<BlobModel<OrderBlobDocument>, OrderBlobDocument>(blobName);
            var model = blob?.Document;

            if (model == null)
            {
                return new CosmosDbResponse(0)
                {
                    DynamicInformations = new Dictionary<string, object>
                    {
                        ["Unfound order blob"] = blobName
                    }
                };
            }

            var transactionId = model.TransactionId;
            var dataRequest = CosmosDbRequest<CosmosDataDocument>.BuildBasedOnPartitionKeyAndId(transactionId, transactionId);
            var dataResponse = await _dataCosmosDb.GetAsync(dataRequest);
            var dataDocument = dataResponse.Documents.SingleOrDefault();
            if (dataDocument == null)
            {
                return new CosmosDbResponse(0)
                {
                    DynamicInformations = new Dictionary<string, object>
                    {
                        ["Found order blob"] = blobName,
                        ["Unfound TransactionId in Cosmos"] = transactionId,
                    }
                };
            }

            return new CosmosDbResponse(dataResponse.RequestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataResponse)] = dataResponse,
                    [nameof(orderId)] = orderId,
                    [nameof(transactionId)] = transactionId,
                    ["Found order blob"] = blobName
                }
            };
        }

        public async Task<ICosmosDbResponse> SaveAsync(IPayload payload, IData data)
        {
            var dataDocument = new CosmosDataDocument(data.Request, data.Response);
            var payloadDocument = new CosmosPayloadDocument(payload.RequestDto, payload.ResponseDto);
           
            var dataCosmosDbRequest = new CosmosDbRequest<CosmosDataDocument>(dataDocument);
            var payloadCosmosDbRequest = new CosmosDbRequest<CosmosPayloadDocument>(payloadDocument);

            var dataTask = _dataCosmosDb.InsertAsync(dataCosmosDbRequest);
            var payloadTask = _payloadCosmosDb.InsertAsync(payloadCosmosDbRequest);

            var ordersBlobsTask = GetSaveOrdersBlobsTask(data);
                
            await Task.WhenAll(dataTask, payloadTask, ordersBlobsTask);

            var dataRequestUnits = (await dataTask).RequestUnits;
            var payloadRequestUnits = (await payloadTask).RequestUnits;
            var requestUnits = dataRequestUnits + payloadRequestUnits;

            var blobs = await ordersBlobsTask;

            return new CosmosDbResponse(requestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataRequestUnits)] = $"{dataRequestUnits} RU",
                    [nameof(payloadRequestUnits)] = $"{payloadRequestUnits} RU",
                    [nameof(blobs)] = $"{blobs} files"
                }
            };
        }

        private async Task<int> GetSaveOrdersBlobsTask(IData data)
        {
            var documents = data.Response
                .Orders
                .Select(x => new OrderBlobDocument
                {
                    OrderId = x.OrderId,
                    Product = x.Product,
                    OrderPrice = x.OrderPrice,
                    TransactionId = data.Response.TransactionId
                });

            var blobs = documents
                .Select(x => new BlobModel<OrderBlobDocument>
                {
                    Document = x,
                    Name = GenerateOrderBlobName(x.OrderId),
                    Metadata = new Dictionary<string, string>
                    {
                        [nameof(Order.OrderId)] = x.OrderId
                    }
                });

            var tasks = blobs
                .Select(x => _blobClient.SaveBlobAsync<BlobModel<OrderBlobDocument>, OrderBlobDocument>(x))
                .ToList();

            await Task.WhenAll(tasks);
            return tasks.Count;
        }

        private static string GenerateOrderBlobName(string orderId) => $"{orderId}.txt";
    }

    public enum Containers
    {
        None,
        Data,
        Payload
    }
}
