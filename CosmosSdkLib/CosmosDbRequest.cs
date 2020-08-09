using Contracts.Ports.CosmosDb;

namespace CosmosSdkLib
{
    public class CosmosDbRequest<TDocument> : ICosmosDbRequest<TDocument> where TDocument : ICosmosDbDocument
    {
        private const string IdField = "id";
        private const string PartitionKeyField = "PartitionKey";

        public CosmosDbRequest(TDocument document)
        {
            Document = document;
        }

        public CosmosDbRequest(string query)
        {
            Query = query;
        }

        public static CosmosDbRequest<TDocument> BuildBasedOnPartitionKey(string partitionKey)
        {
            var query = $"SELECT VALUE c FROM c WHERE c.{PartitionKeyField} = \"{partitionKey}\"";
            return new CosmosDbRequest<TDocument>(query);
        }

        public static CosmosDbRequest<TDocument> BuildBasedOnPartitionKeyAndId(string partitionKey, string id)
        {
            var query = $"SELECT VALUE c FROM c WHERE c.{PartitionKeyField} = \"{partitionKey}\" and c.{IdField} = \"{id}\"";
            return new CosmosDbRequest<TDocument>(query);
        }

        public string Query { get; }

        public TDocument Document { get; }

        object ICosmosDbRequest.Document => Document;
    }

    public class CosmosDbRequest : CosmosDbRequest<ICosmosDbDocument>
    {
        public CosmosDbRequest(ICosmosDbDocument document) : base(document)
        {
        }

        public CosmosDbRequest(string query) : base(query)
        {
        }
    }
}
