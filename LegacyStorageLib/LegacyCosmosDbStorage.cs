using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contracts.Ports.Configuration;
using Contracts.Ports.CosmosDb;
using Contracts.Ports.CosmosDb.Documents;
using CosmosSdkLib;
using LegacyStorageLib.Documents;

namespace LegacyStorageLib
{
    public class LegacyCosmosDbStorage : ICosmosDbStorage
    {
        private readonly CosmosDbClient<CosmosDataDocument> _dataCosmosDb;
        private readonly CosmosDbClient<CosmosPayloadDocument> _payloadCosmosDb;
        private readonly CosmosDbClient<CosmosCorrelationDocument> _correlationOrderIdCosmosDb;

        public LegacyCosmosDbStorage(ICosmosSettings settings)
        {
            _dataCosmosDb = new CosmosDbClient<CosmosDataDocument>(settings, nameof(Containers.Data));
            _payloadCosmosDb = new CosmosDbClient<CosmosPayloadDocument>(settings, nameof(Containers.Payload));
            _correlationOrderIdCosmosDb = new CosmosDbClient<CosmosCorrelationDocument>(settings, nameof(Containers.Correlation));
        }

        public async Task<ICosmosDbResponse> GetAsync(string orderId)
        {
            var orderIdRequest = CosmosDbRequest<CosmosCorrelationDocument>.BuildBasedOnPartitionKey(orderId);
            var orderIdResponse = await _correlationOrderIdCosmosDb.GetAsync(orderIdRequest);
            var orderIdDocument = orderIdResponse.Documents.SingleOrDefault();
            if (orderIdDocument == null)
            {
                return new CosmosDbResponse(orderIdResponse.RequestUnits);
            }

            var transactionId = orderIdDocument.TransactionId;
            var dataRequest = CosmosDbRequest<CosmosDataDocument>.BuildBasedOnPartitionKeyAndId(transactionId, transactionId);
            var dataResponse = await _dataCosmosDb.GetAsync(dataRequest);
            var dataDocument = dataResponse.Documents.SingleOrDefault();
            if (dataDocument == null)
            {
                return new CosmosDbResponse(orderIdResponse.RequestUnits);
            }

            var requestUnits = orderIdResponse.RequestUnits + dataResponse.RequestUnits;
            return new CosmosDbResponse(requestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(orderIdResponse)] = orderIdResponse,
                    [nameof(dataResponse)] = dataResponse
                }
            };
        }

        public async Task<ICosmosDbResponse> SaveAsync(IPayload payload, IData data)
        {
            var transactionId = data.Request.TransactionId;
            var orderIds = data.Response.Orders.Select(x => x.OrderId).ToList();

            var dataDocument = new CosmosDataDocument(data.Request, data.Response);
            var payloadDocument = new CosmosPayloadDocument(payload.RequestDto, payload.ResponseDto);
            var correlationDocuments = orderIds
                .Select(orderId => new CosmosCorrelationDocument(transactionId, orderId))
                .ToList();

            var dataCosmosDbRequest = new CosmosDbRequest<CosmosDataDocument>(dataDocument);
            var payloadCosmosDbRequest = new CosmosDbRequest<CosmosPayloadDocument>(payloadDocument);
            var correlationCosmosDbRequests = correlationDocuments
                .Select(document => new CosmosDbRequest<CosmosCorrelationDocument>(document))
                .ToList();
                
            var dataTask = _dataCosmosDb.InsertAsync(dataCosmosDbRequest);
            var payloadTask = _payloadCosmosDb.InsertAsync(payloadCosmosDbRequest);
            var correlationTasks = correlationCosmosDbRequests
                .Select(request => _correlationOrderIdCosmosDb.InsertAsync(request))
                .ToList();

            var tasks = new List<Task>(correlationTasks)
            {
                dataTask,
                payloadTask
            };

            await Task.WhenAll(tasks);

            var dataRequestUnits = (await dataTask).RequestUnits;
            var payloadRequestUnits = (await payloadTask).RequestUnits;
            var correlationRequestUnits = await GetRequestUnitsAsync(correlationTasks);

            var requestUnits = dataRequestUnits + payloadRequestUnits + correlationRequestUnits;

            return new CosmosDbResponse(requestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataRequestUnits)] = $"{dataRequestUnits} RU",
                    [nameof(payloadRequestUnits)] = $"{payloadRequestUnits} RU",
                    [nameof(correlationRequestUnits)] = $"{correlationRequestUnits} RU",
                }
            };
        }

        private static async Task<double> GetRequestUnitsAsync(IEnumerable<Task<ICosmosDbResponse<CosmosCorrelationDocument>>> tasks)
        {
            double requestUnits = 0;
            
            foreach (var task in tasks)
            {
                requestUnits += (await task).RequestUnits;
            }

            return Math.Round(requestUnits, 3);
        }
    }

    public enum Containers
    {
        None,
        Data,
        Payload,
        Correlation
    }
}
