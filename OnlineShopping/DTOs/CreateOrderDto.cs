using System.ComponentModel.DataAnnotations;

namespace OnlineShopping.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();

        public decimal ShippingCost { get; set; } = 10; // Default shipping cost

        // Optional: Allow manual promotion code entry
        public string? PromoCode { get; set; }
    }
}