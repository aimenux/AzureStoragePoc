using Contracts.Ports.CosmosDb;

namespace LegacyStorageLib.Documents
{
    public class CosmosCorrelationDocument : ICosmosDbDocument
    {
        public string Id => TransactionId;

        public string PartitionKey => OrderId;

        public string OrderId { get; }

        public string TransactionId { get; }

        public CosmosCorrelationDocument(string transactionId, string orderId)
        {
            TransactionId = transactionId;
            OrderId = orderId;
        }
    }
}