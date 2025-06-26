using OnlineShopping.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineShopping.DTOs
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus NewStatus { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public string? ChangedBy { get; set; }
    }
}