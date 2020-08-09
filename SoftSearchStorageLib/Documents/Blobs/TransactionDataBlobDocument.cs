using BlobSdkLib;
using Contracts.Models;

namespace SoftSearchStorageLib.Documents.Blobs
{
    [BlobContainer(ContainerName = "Datas")]
    public class TransactionDataBlobDocument
    {
        public string TransactionId => Request.TransactionId;

        public Request Request { get; set; }

        public Response Response { get; set; }

        public TransactionDataBlobDocument(Request request, Response response)
        {
            Request = request;
            Response = response;
        }
    }
}
