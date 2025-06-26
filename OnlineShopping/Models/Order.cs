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
        public DateTime? LastStatusUpdate { get; set; }

        // Original amount before discounts
        public decimal SubTotal { get; set; }

        // Total discount applied to the order
        public decimal TotalDiscount { get; set; }

        // Shipping cost
        public decimal ShippingCost { get; set; }

        // Final amount after discounts and shipping
        public decimal TotalAmount => SubTotal - TotalDiscount + ShippingCost;

        // Navigation properties
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<AppliedDiscount> AppliedDiscounts { get; set; } = new List<AppliedDiscount>();
        public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    }
}