using Newtonsoft.Json;

namespace Contracts.Ports.CosmosDb
{
    public interface ICosmosDbDocument
    {
        [JsonProperty("id")]
        string Id { get; }

        string PartitionKey { get; }
    }
}