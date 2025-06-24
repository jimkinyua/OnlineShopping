using OnlineShopping.Data;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public class OrderManagement : IOrderManagement
    {
        private readonly OrderDbContext _context;

        public OrderManagement(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(int customerId, decimal totalAmount)
        {
            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow.Ticks}",
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalAmount = totalAmount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order;
        }
    }
}