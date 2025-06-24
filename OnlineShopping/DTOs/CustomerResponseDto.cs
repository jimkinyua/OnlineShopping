using OnlineShopping.Models;

namespace OnlineShopping.DTOs
{
    public class CustomerResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public CustomerSegment Segment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}