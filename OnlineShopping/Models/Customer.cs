namespace OnlineShopping.Models
{
    public enum CustomerSegment
    {
        Regular,
        Premium,
        VIP
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public CustomerSegment Segment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // can have many orders
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}