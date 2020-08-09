namespace Contracts.Models
{
    public class Order
    {
        public string OrderId { get; set; }
        public Product Product { get; set; }
        public Merchant Merchant { get; set; }
        public ProductTax ProductTax { get; set; }
        public decimal OrderPrice => Product.Quantity * Product.UnitPrice * ProductTax.TaxRate;
    }
}
