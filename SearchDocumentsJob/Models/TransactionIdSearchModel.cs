using System;
using System.Collections.Generic;
using Contracts.Ports.SearchService;

namespace SearchDocumentsJob.Models
{
    public class TransactionIdSearchModel : ISearchModel
    {
        public string TransactionId { get; set; }

        public ICollection<string> OrderIds { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}