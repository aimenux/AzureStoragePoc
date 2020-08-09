using Contracts.Ports.CosmosDb;

namespace CosmosSdkLib
{
    public class CosmosDbDocument : ICosmosDbDocument
    {
        public CosmosDbDocument()
        {
        }

        public CosmosDbDocument(string id, string partitionKey)
        {
            Id = id;
            PartitionKey = partitionKey;
        }

        public string Id { get; set; }
        public string PartitionKey { get; set; }
    }
}
