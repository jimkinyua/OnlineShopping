using OnlineShopping.Models;

namespace OnlineShopping.DTOs
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public CustomerSegment CustomerSegment { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal SubtotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<AppliedDiscountDto> AppliedDiscounts { get; set; } = new List<AppliedDiscountDto>();
    }

    public class AppliedDiscountDto
    {
        public string PromotionName { get; set; } = string.Empty;
        public string PromotionDescription { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }
}