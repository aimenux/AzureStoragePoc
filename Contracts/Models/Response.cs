using System;
using System.Collections.Generic;

namespace Contracts.Models
{
    public class Response
    {
        public string TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}
