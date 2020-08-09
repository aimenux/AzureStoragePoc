using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlobSdkLib;
using Contracts.Models;
using Contracts.Ports.BlobStorage;
using Contracts.Ports.Configuration;
using Contracts.Ports.CosmosDb;
using Contracts.Ports.CosmosDb.Documents;
using CosmosSdkLib;
using SoftBlobStorageLib.Documents.Blobs;
using SoftBlobStorageLib.Documents.Cosmos;

namespace SoftBlobStorageLib
{
    public class SoftBlobCosmosDbStorage : ICosmosDbStorage
    {
        private readonly IBlobClient _blobClient;
        private readonly CosmosDbClient<SoftCosmosDataDocument> _dataCosmosDb;
        private readonly CosmosDbClient<SoftCosmosPayloadDocument> _payloadCosmosDb;

        public SoftBlobCosmosDbStorage(ISettings settings)
        {
            _blobClient = new BlobClient(settings.BlobSettings);
            _dataCosmosDb = new CosmosDbClient<SoftCosmosDataDocument>(settings.CosmosSettings, nameof(Containers.Data));
            _payloadCosmosDb = new CosmosDbClient<SoftCosmosPayloadDocument>(settings.CosmosSettings, nameof(Containers.Payload));
        }

        public async Task<ICosmosDbResponse> GetAsync(string orderId)
        {
            var orderBlobName = GenerateOrderBlobName(orderId);
            var orderBlob = await _blobClient.GetBlobAsync<BlobModel<OrderBlobDocument>, OrderBlobDocument>(orderBlobName);
            var orderBlobDocument = orderBlob?.Document;

            if (orderBlobDocument == null)
            {
                return new CosmosDbResponse(0)
                {
                    DynamicInformations = new Dictionary<string, object>
                    {
                        ["Unfound order blob"] = orderBlobName
                    }
                };
            }

            var transactionId = orderBlobDocument.TransactionId;
            var dataRequest = CosmosDbRequest<SoftCosmosDataDocument>.BuildBasedOnPartitionKeyAndId(transactionId, transactionId);
            var dataResponse = await _dataCosmosDb.GetAsync(dataRequest);
            var dataDocument = dataResponse.Documents.SingleOrDefault();
            if (dataDocument == null)
            {
                return new CosmosDbResponse(0)
                {
                    DynamicInformations = new Dictionary<string, object>
                    {
                        ["Found order blob"] = orderBlobName,
                        ["Unfound TransactionId in Cosmos"] = transactionId
                    }
                };
            }

            var dataBlobName = dataDocument.AttachmentBlobName;
            var dataBlob = await _blobClient.GetBlobAsync<BlobModel<TransactionDataBlobDocument>, TransactionDataBlobDocument>(dataBlobName);
            var orderedProduct = dataBlob.Document
                .Response
                .Orders
                .Single(x => x.OrderId == orderId);
            var orderPrice = orderedProduct.OrderPrice;

            return new CosmosDbResponse(dataResponse.RequestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataResponse)] = dataResponse,
                    [nameof(orderId)] = orderId,
                    [nameof(transactionId)] = transactionId,
                    [nameof(orderPrice)] = orderPrice,
                    ["Found order blob"] = orderBlobName
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
            
            var orderBlobsTask = GetSaveOrderBlobsTask(data);
            var transactionBlobTask = GetSaveTransactionBlobTask(payload, data);

            await Task.WhenAll(dataTask, payloadTask, orderBlobsTask, transactionBlobTask);

            var dataRequestUnits = (await dataTask).RequestUnits;
            var payloadRequestUnits = (await payloadTask).RequestUnits;
            var requestUnits = dataRequestUnits + payloadRequestUnits;

            var blobs = (await orderBlobsTask) + (await transactionBlobTask);

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

        private async Task<int> GetSaveTransactionBlobTask(IPayload payload, IData data)
        {
            var transactionId = data.Request.TransactionId;
            var name = GenerateTransactionBlobName(transactionId);
            var nbrProducts = data.Request.Products.Count.ToString();
            var metadata = new Dictionary<string, string>
            {
                [nameof(nbrProducts)] = nbrProducts
            };

            var dataBlobDocument = new TransactionDataBlobDocument(data.Request, data.Response);
            var dataBlobModel = new BlobModel<TransactionDataBlobDocument>
            {
                Name = name,
                Metadata = metadata,
                Document = dataBlobDocument
            };

            var payloadBlobDocument = new TransactionPayloadBlobDocument(payload.RequestDto, payload.ResponseDto);
            var payloadBlobModel = new BlobModel<TransactionPayloadBlobDocument>
            {
                Name = name,
                Metadata = metadata,
                Document = payloadBlobDocument,
            };

            var dataBlobTask = _blobClient.SaveBlobAsync<BlobModel<TransactionDataBlobDocument>,TransactionDataBlobDocument>(dataBlobModel);
            var payloadBlobTask = _blobClient.SaveBlobAsync<BlobModel<TransactionPayloadBlobDocument>,TransactionPayloadBlobDocument>(payloadBlobModel);
            var tasks = new[] {dataBlobTask, payloadBlobTask};

            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        private async Task<int> GetSaveOrderBlobsTask(IData data)
        {
            var documents = data.Response
                .Orders
                .Select(x => new OrderBlobDocument
                {
                    Product = x.Product,
                    OrderId = x.OrderId,
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
        private static string GenerateTransactionBlobName(string transactionId) => $"{transactionId}.txt";
    }

    public enum Containers
    {
        None,
        Data,
        Payload
    }
}
