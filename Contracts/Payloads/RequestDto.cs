using System;
using System.Collections.Generic;

namespace Contracts.Payloads
{
    public class RequestDto
    {
        public string TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public ClientDto Client { get; set; }
        public ICollection<ProductDto> Products { get; set; }
    }
}
