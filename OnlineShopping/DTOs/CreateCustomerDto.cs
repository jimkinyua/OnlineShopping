using OnlineShopping.Models;

namespace OnlineShopping.DTOs
{
    public class CreateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public CustomerSegment Segment { get; set; } = CustomerSegment.Regular;
    }
}