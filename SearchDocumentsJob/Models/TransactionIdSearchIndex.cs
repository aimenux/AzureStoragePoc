using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Contracts.Ports.SearchService;
using Microsoft.Azure.Search;

namespace SearchDocumentsJob.Models
{
    public class TransactionIdSearchIndex : ISearchIndex
    {
        [Key]
        [IsFilterable]
        public string TransactionId { get; set; }

        [IsFilterable]
        public ICollection<string> OrderIds { get; set; }

        [IsFilterable, IsSortable]
        public DateTime TransactionDate { get; set; }
    }
}