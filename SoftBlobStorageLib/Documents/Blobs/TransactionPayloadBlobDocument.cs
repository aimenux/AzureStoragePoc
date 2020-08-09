using BlobSdkLib;
using Contracts.Payloads;

namespace SoftBlobStorageLib.Documents.Blobs
{
    [BlobContainer(ContainerName = "SoftPayloads")]
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
