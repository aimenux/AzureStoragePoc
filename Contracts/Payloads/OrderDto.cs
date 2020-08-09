namespace Contracts.Payloads
{
    public class OrderDto
    {
        public string OrderId { get; set; }
        public decimal OrderPrice { get; set; }
        public ProductDto Product { get; set; }
    }
}