namespace OnlineShopping.Models
{
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal SubtotalAmount { get; set; } // Original amount before discounts
        public decimal TotalDiscount { get; set; } // Total discount applied
        public decimal TotalAmount { get; set; } // Final amount after discounts
        public List<AppliedDiscount> AppliedDiscounts { get; set; } = new List<AppliedDiscount>();
    }
}