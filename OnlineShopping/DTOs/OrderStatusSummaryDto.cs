using OnlineShopping.Models;

namespace OnlineShopping.DTOs
{
    public class OrderStatusSummaryDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public DateTime? LastStatusUpdate { get; set; }
        public int DaysInCurrentStatus => LastStatusUpdate.HasValue
            ? (int)(DateTime.UtcNow - LastStatusUpdate.Value).TotalDays
            : 0;
    }
}