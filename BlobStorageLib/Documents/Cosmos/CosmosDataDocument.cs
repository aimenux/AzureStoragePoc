using Contracts.Models;
using Contracts.Ports.CosmosDb;

namespace BlobStorageLib.Documents.Cosmos
{
    public class CosmosDataDocument : ICosmosDbDocument
    {
        public Request Request { get; }

        public Response Response { get; }

        public string Id => Request.TransactionId;

        public string PartitionKey => Request.TransactionId;

        public CosmosDataDocument(Request request, Response response)
        {
            Request = request;
            Response = response;
        }
    }
}