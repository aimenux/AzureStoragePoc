using System;
using Contracts.Ports.SearchService;

namespace SearchStorageLib.SearchModels
{
    public class OrderSearchModel : ISearchModel
    {
        public string OrderId { get; set; }
        public string TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
