using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Payloads;
using Contracts.Ports.CosmosDb;

namespace SoftBlobStorageLib.Documents.Cosmos
{
    public class SoftCosmosPayloadDocument : ICosmosDbDocument
    {
        public string Id { get; }

        public string PartitionKey { get; }

        public DateTime TransactionDate { get; set; }

        public string AttachmentBlobName { get; set; }

        public ICollection<SoftWeightOrderDto> Orders { get; set; }

        public SoftCosmosPayloadDocument()
        {
        }

        public SoftCosmosPayloadDocument(RequestDto request, ResponseDto response, string blobName)
        {
            Id = request.TransactionId;
            TransactionDate = request.TransactionDate;
            PartitionKey = request.TransactionId;
            AttachmentBlobName = blobName;
            Orders = response
                .Orders
                .Select(x => new SoftWeightOrderDto(x))
                .ToList();
        }
    }
}