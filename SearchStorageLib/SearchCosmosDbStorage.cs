using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contracts.Extensions;
using Contracts.Ports.Configuration;
using Contracts.Ports.CosmosDb;
using Contracts.Ports.CosmosDb.Documents;
using Contracts.Ports.SearchService;
using CosmosSdkLib;
using Polly;
using SearchSdkLib;
using SearchStorageLib.Documents;
using SearchStorageLib.Exceptions;
using SearchStorageLib.SearchIndexes;
using SearchStorageLib.SearchModels;

namespace SearchStorageLib
{
    public class SearchCosmosDbStorage : ICosmosDbStorage
    {
        private readonly ISearchClient<OrderSearchIndex> _searchClient;
        private readonly CosmosDbClient<CosmosDataDocument> _dataCosmosDb;
        private readonly CosmosDbClient<CosmosPayloadDocument> _payloadCosmosDb;

        public SearchCosmosDbStorage(ISettings settings)
        {
            _searchClient = new SearchClient<OrderSearchIndex>(settings.SearchSettings);
            _dataCosmosDb = new CosmosDbClient<CosmosDataDocument>(settings.CosmosSettings, nameof(Containers.Data));
            _payloadCosmosDb = new CosmosDbClient<CosmosPayloadDocument>(settings.CosmosSettings, nameof(Containers.Payload));
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
            var dataRequest = CosmosDbRequest<CosmosDataDocument>.BuildBasedOnPartitionKeyAndId(transactionId, transactionId);
            var dataResponse = await _dataCosmosDb.GetAsync(dataRequest);
            var dataDocument = dataResponse.Documents.SingleOrDefault();
            if (dataDocument == null)
            {
                return new CosmosDbResponse(0)
                {
                    DynamicInformations = new Dictionary<string, object>
                    {
                        ["Found OrderId in Search"] = orderId,
                        ["Unfound TransactionId in Cosmos"] = transactionId
                    }
                };
            }

            return new CosmosDbResponse(dataResponse.RequestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataResponse)] = dataResponse,
                    [nameof(orderId)] = orderId,
                    [nameof(transactionId)] = transactionId
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

            var searchModels = BuildOrderSearchModels(data);
            var searchTask = _searchClient.SaveAsync(searchModels);
                
            await Task.WhenAll(dataTask, payloadTask, searchTask);

            var dataRequestUnits = (await dataTask).RequestUnits;
            var payloadRequestUnits = (await payloadTask).RequestUnits;
            var requestUnits = dataRequestUnits + payloadRequestUnits;

            return new CosmosDbResponse(requestUnits)
            {
                DynamicInformations = new Dictionary<string, object>
                {
                    [nameof(dataRequestUnits)] = $"{dataRequestUnits} RU",
                    [nameof(payloadRequestUnits)] = $"{payloadRequestUnits} RU"
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
    }

    public enum Containers
    {
        None,
        Data,
        Payload
    }
}
