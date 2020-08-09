using BlobSdkLib;
using Contracts.Payloads;

namespace SoftSearchStorageLib.Documents.Blobs
{
    [BlobContainer(ContainerName = "Payloads")]
    public class TransactionPayloadBlobDocument
    {
        public string TransactionId => Request.TransactionId;

        public RequestDto Request { get; set; }

        public ResponseDto Response { get; set; }

        public TransactionPayloadBlobDocument(RequestDto requestDto, ResponseDto responseDto)
        {
            Request = requestDto;
            Response = responseDto;
        }
    }
}
