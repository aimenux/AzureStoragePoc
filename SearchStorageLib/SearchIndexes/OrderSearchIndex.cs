using System;
using System.ComponentModel.DataAnnotations;
using Contracts.Ports.SearchService;
using Microsoft.Azure.Search;

namespace SearchStorageLib.SearchIndexes
{
    public class OrderSearchIndex : ISearchIndex
    {
        [Key]
        [IsFilterable, IsSearchable]
        public string OrderId { get; set; }

        [IsFilterable, IsSearchable]
        public string TransactionId { get; set; }

        [IsFilterable, IsSortable]
        public DateTime TransactionDate { get; set; }
    }
}
