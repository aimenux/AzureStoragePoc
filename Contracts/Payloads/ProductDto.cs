namespace Contracts.Payloads
{
    public class ProductDto
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ProductType { get; set; }
    }
}