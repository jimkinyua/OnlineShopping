using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.DTOs;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public class OrderManagement : IOrderManagement
    {
        private readonly OrderDbContext _context;
        private readonly IDiscountService _discountService;
        private readonly OrderStatusTransitionValidator _statusValidator;

        public OrderManagement(OrderDbContext context, IDiscountService discountService, OrderStatusTransitionValidator statusValidator)
        {
            _context = context;
            _discountService = discountService;
            _statusValidator = statusValidator;
        }

        public async Task<Order> CreateOrderAsync(CreateOrderDto orderDto)
        {
            // Validate customer exists
            var customer = await _context.Customers.FindAsync(orderDto.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer with ID {orderDto.CustomerId} not found.");
            }

            // Calculate subtotal from items
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var itemDto in orderDto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                {
                    throw new ArgumentException($"Product with ID {itemDto.ProductId} not found.");
                }

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    ItemDiscount = 0 // Individual item discounts can be added later
                };

                subTotal += orderItem.UnitPrice * orderItem.Quantity;
                orderItems.Add(orderItem);
            }

            // Calculate discounts
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                orderDto.CustomerId,
                subTotal,
                orderItems);

            // Check for free shipping promotions
            var promotions = await _discountService.GetApplicablePromotionsAsync(orderDto.CustomerId, subTotal);
            var hasFreeShipping = promotions.Any(p => p.Type == PromotionType.FreeShipping);
            var shippingCost = hasFreeShipping ? 0 : orderDto.ShippingCost;

            // Create the order
            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow.Ticks}",
                CustomerId = customer.Id,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                LastStatusUpdate = DateTime.UtcNow,
                SubTotal = subTotal,
                TotalDiscount = totalDiscount,
                ShippingCost = shippingCost,
                OrderItems = orderItems
            };

            // Add applied discounts to the order
            foreach (var discount in appliedDiscounts)
            {
                discount.Order = order;
                order.AppliedDiscounts.Add(discount);
            }

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
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.AppliedDiscounts)
                    .ThenInclude(ad => ad.PromotionRule)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.AppliedDiscounts)
                    .ThenInclude(ad => ad.PromotionRule)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.AppliedDiscounts)
                    .ThenInclude(ad => ad.PromotionRule)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<OrderResponseDto> GetOrderDetailsAsync(int orderId)
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found.");
            }

            var allowedTransitions = _statusValidator.GetAllowedTransitions(order.Status).ToList();

            return new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.Name,
                CustomerSegment = order.Customer.Segment.ToString(),
                OrderDate = order.OrderDate,
                Status = order.Status,
                LastStatusUpdate = order.LastStatusUpdate,
                SubTotal = order.SubTotal,
                TotalDiscount = order.TotalDiscount,
                ShippingCost = order.ShippingCost,
                TotalAmount = order.TotalAmount,
                OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ItemDiscount = oi.ItemDiscount,
                    LineTotal = oi.LineTotal
                }).ToList(),
                AppliedDiscounts = order.AppliedDiscounts.Select(ad => new AppliedDiscountDto
                {
                    Id = ad.Id,
                    PromotionName = ad.PromotionRule.Name,
                    Description = ad.PromotionRule.Description,
                    DiscountAmount = ad.DiscountAmount,
                    AppliedAt = ad.AppliedAt
                }).ToList(),
                StatusHistory = order.StatusHistory.Select(sh => new OrderStatusHistoryDto
                {
                    Id = sh.Id,
                    PreviousStatus = sh.PreviousStatus,
                    NewStatus = sh.NewStatus,
                    ChangedAt = sh.ChangedAt,
                    ChangedBy = sh.ChangedBy,
                    Comment = sh.Comment
                }).ToList(),
                AllowedTransitions = allowedTransitions
            };
        }

        public async Task<List<PromotionRule>> GetAvailablePromotionsAsync(int customerId, decimal orderAmount)
        {
            return await _discountService.GetApplicablePromotionsAsync(customerId, orderAmount);
        }

        public async Task<decimal> CalculateOrderTotalAsync(int customerId, List<OrderItem> items)
        {
            var subTotal = items.Sum(i => i.UnitPrice * i.Quantity - i.ItemDiscount);
            var (totalDiscount, _) = await _discountService.CalculateDiscountsAsync(customerId, subTotal, items);

            // Default shipping cost
            const decimal defaultShipping = 10;

            // Check for free shipping
            var promotions = await _discountService.GetApplicablePromotionsAsync(customerId, subTotal);
            var hasFreeShipping = promotions.Any(p => p.Type == PromotionType.FreeShipping);
            var shippingCost = hasFreeShipping ? 0 : defaultShipping;

            return subTotal - totalDiscount + shippingCost;
        }
    }
}