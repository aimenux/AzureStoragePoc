namespace Contracts.Payloads
{
    public class SoftWeightOrderDto
    {
        public string OrderId { get; set; }
        public ProductDto Product { get; set; }
        public decimal OrderPrice { get; set; }

        public SoftWeightOrderDto()
        {
        }

        public SoftWeightOrderDto(OrderDto order)
        {
            OrderId = order.OrderId;
            Product = order.Product;
            OrderPrice = order.OrderPrice;
        }
    }
}