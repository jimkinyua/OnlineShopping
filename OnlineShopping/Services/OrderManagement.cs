using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public class OrderManagement : IOrderManagement
    {
        private readonly OrderDbContext _context;
        private readonly IDiscountService _discountService;

        public OrderManagement(OrderDbContext context, IDiscountService discountService)
        {
            _context = context;
            _discountService = discountService;
        }

        public async Task<Order> CreateOrderAsync(int customerId, decimal subtotalAmount)
        {
            // Calculate applicable discounts
            var appliedDiscounts = await _discountService.CalculateDiscountsAsync(customerId, subtotalAmount);
            var totalDiscount = await _discountService.GetTotalDiscountAmountAsync(appliedDiscounts);
            var finalAmount = subtotalAmount - totalDiscount;

            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow.Ticks}",
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                SubtotalAmount = subtotalAmount,
                TotalDiscount = totalDiscount,
                TotalAmount = finalAmount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Save applied discounts
            foreach (var discount in appliedDiscounts)
            {
                discount.OrderId = order.Id;
                _context.AppliedDiscounts.Add(discount);
            }

            if (appliedDiscounts.Any())
            {
                await _context.SaveChangesAsync();
            }

            var orderWithDiscounts = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.AppliedDiscounts)
                .ThenInclude(d => d.Promotion)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return orderWithDiscounts!;
        }
    }
}