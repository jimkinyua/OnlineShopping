using Microsoft.EntityFrameworkCore;
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
                LastStatusUpdate = DateTime.UtcNow,
                TotalAmount = totalAmount
            };

            // Create initial status history entry
            var initialHistory = new OrderStatusHistory
            {
                Order = order,
                PreviousStatus = OrderStatus.Pending,
                NewStatus = OrderStatus.Pending,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "System",
                Comment = "Order created"
            };

            _context.Orders.Add(order);
            _context.OrderStatusHistory.Add(initialHistory);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}