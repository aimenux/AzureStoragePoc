using BlobSdkLib;
using Contracts.Models;

namespace SoftBlobStorageLib.Documents.Blobs
{
    [BlobContainer(ContainerName = "SoftOrders")]
    public class OrderBlobDocument
    {
        public Product Product { get; set; }

        public string OrderId { get; set; }

        public decimal OrderPrice { get; set; }

        public string TransactionId { get; set; }
    }
}
