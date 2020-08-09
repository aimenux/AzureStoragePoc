using BlobSdkLib;
using Contracts.Models;

namespace BlobStorageLib.Documents.Blobs
{
    [BlobContainer(ContainerName = "Orders")]
    public class OrderBlobDocument
    {
        public Product Product { get; set; }

        public string OrderId { get; set; }

        public decimal OrderPrice { get; set; }

        public string TransactionId { get; set; }
    }
}
