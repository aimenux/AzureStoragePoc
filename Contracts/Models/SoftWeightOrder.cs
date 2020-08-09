namespace Contracts.Models
{
    public class SoftWeightOrder
    {
        public string OrderId { get; set; }
        public Product Product { get; set; }
        public decimal OrderPrice { get; set; }

        public SoftWeightOrder()
        {
        }

        public SoftWeightOrder(Order order)
        {
            OrderId = order.OrderId;
            Product = order.Product;
            OrderPrice = order.OrderPrice;
        }
    }
}