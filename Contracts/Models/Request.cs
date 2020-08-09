using System;
using System.Collections.Generic;

namespace Contracts.Models
{
    public class Request
    {
        public string TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public Client Client { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
