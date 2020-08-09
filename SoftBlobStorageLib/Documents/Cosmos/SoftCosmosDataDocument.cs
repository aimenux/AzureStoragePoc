using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Models;
using Contracts.Ports.CosmosDb;

namespace SoftBlobStorageLib.Documents.Cosmos
{
    public class SoftCosmosDataDocument : ICosmosDbDocument
    {
        public string Id { get; }

        public string PartitionKey { get; }

        public DateTime TransactionDate { get; set; }

        public string AttachmentBlobName { get; set; }

        public ICollection<SoftWeightOrder> Orders { get; set; }

        public SoftCosmosDataDocument()
        {
        }

        public SoftCosmosDataDocument(Request request, Response response, string blobName)
        {
            Id = request.TransactionId;
            PartitionKey = request.TransactionId;
            TransactionDate = request.TransactionDate;
            AttachmentBlobName = blobName;
            Orders = response
                .Orders
                .Select(x => new SoftWeightOrder(x))
                .ToList();
        }
    }
}