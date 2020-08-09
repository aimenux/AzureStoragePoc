using Contracts.Payloads;
using Contracts.Ports.CosmosDb;

namespace SearchStorageLib.Documents
{
    public class CosmosPayloadDocument : ICosmosDbDocument
    {
        public RequestDto Request { get; }

        public ResponseDto Response { get; }

        public string Id => Request.TransactionId;

        public string PartitionKey => Request.TransactionId;

        public CosmosPayloadDocument(RequestDto request, ResponseDto response)
        {
            Request = request;
            Response = response;
        }
    }
}