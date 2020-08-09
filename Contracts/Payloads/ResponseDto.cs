using System;
using System.Collections.Generic;

namespace Contracts.Payloads
{
    public class ResponseDto
    {
        public string TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public ICollection<OrderDto> Orders { get; set; }
    }
}
