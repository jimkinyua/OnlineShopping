namespace OnlineShopping.DTOs
{
    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
    }
}