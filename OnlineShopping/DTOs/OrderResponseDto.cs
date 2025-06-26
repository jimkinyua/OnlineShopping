using OnlineShopping.Models;

namespace OnlineShopping.DTOs
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerSegment { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime? LastStatusUpdate { get; set; }

        // Financial details
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }

        // Order items
        public List<OrderItemResponseDto> OrderItems { get; set; } = new();

        // Applied discounts
        public List<AppliedDiscountDto> AppliedDiscounts { get; set; } = new();

        // Status history
        public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
        public List<OrderStatus> AllowedTransitions { get; set; } = new();
    }

    public class OrderStatusHistoryDto
    {
        public int Id { get; set; }
        public OrderStatus PreviousStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
        public string? Comment { get; set; }
    }
}