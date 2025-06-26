using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.DTOs;
using OnlineShopping.Models;
using Microsoft.Extensions.Caching.Memory;

namespace OnlineShopping.Services
{
    public interface IOrderStatusService
    {
        Task<OrderResponseDto?> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateDto);
        Task<OrderResponseDto?> GetOrderWithStatusHistoryAsync(int orderId);
        Task<List<OrderStatusSummaryDto>> GetOrdersByStatusAsync(OrderStatus status);
        Task<Dictionary<OrderStatus, int>> GetOrderStatusStatisticsAsync();
        Task<List<OrderStatusSummaryDto>> GetStaleOrdersAsync(int daysThreshold = 7);
    }

    public class OrderStatusService : IOrderStatusService
    {
        private readonly OrderDbContext _context;
        private readonly IOrderStatusTransitionValidator _transitionValidator;
        private readonly IMemoryCache _cache;
        private const string STATS_CACHE_KEY = "order_status_stats";
        private const int STATS_CACHE_MINUTES = 5;

        public OrderStatusService(
            OrderDbContext context,
            IOrderStatusTransitionValidator transitionValidator,
            IMemoryCache cache)
        {
            _context = context;
            _transitionValidator = transitionValidator;
            _cache = cache;
        }

        public async Task<OrderResponseDto?> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.StatusHistory)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return null;

                // Validate transition
                if (!_transitionValidator.IsValidTransition(order.Status, updateDto.NewStatus))
                {
                    throw new InvalidOperationException(
                        _transitionValidator.GetInvalidTransitionMessage(order.Status, updateDto.NewStatus));
                }

                // Create status history entry
                var historyEntry = new OrderStatusHistory
                {
                    OrderId = orderId,
                    PreviousStatus = order.Status,
                    NewStatus = updateDto.NewStatus,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = updateDto.ChangedBy,
                    Comment = updateDto.Comment
                };

                // Update order
                order.Status = updateDto.NewStatus;
                order.LastStatusUpdate = DateTime.UtcNow;

                _context.OrderStatusHistory.Add(historyEntry);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache
                _cache.Remove(STATS_CACHE_KEY);

                return MapToOrderResponseDto(order);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderResponseDto?> GetOrderWithStatusHistoryAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.StatusHistory.OrderByDescending(h => h.ChangedAt))
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return order != null ? MapToOrderResponseDto(order) : null;
        }

        public async Task<List<OrderStatusSummaryDto>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _context.Orders
                .Where(o => o.Status == status)
                .Select(o => new OrderStatusSummaryDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    LastStatusUpdate = o.LastStatusUpdate
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<OrderStatus, int>> GetOrderStatusStatisticsAsync()
        {
            return await _cache.GetOrCreateAsync(STATS_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(STATS_CACHE_MINUTES);

                return await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count);
            });
        }

        public async Task<List<OrderStatusSummaryDto>> GetStaleOrdersAsync(int daysThreshold = 7)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(-daysThreshold);

            return await _context.Orders
                .Where(o => o.Status != OrderStatus.Delivered &&
                           o.Status != OrderStatus.Cancelled &&
                           (o.LastStatusUpdate == null || o.LastStatusUpdate < thresholdDate))
                .Select(o => new OrderStatusSummaryDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    LastStatusUpdate = o.LastStatusUpdate
                })
                .AsNoTracking()
                .ToListAsync();
        }

        private OrderResponseDto MapToOrderResponseDto(Order order)
        {
            var dto = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name ?? "Unknown",
                OrderDate = order.OrderDate,
                Status = order.Status,
                LastStatusUpdate = order.LastStatusUpdate,
                TotalAmount = order.TotalAmount,
                StatusHistory = order.StatusHistory
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new OrderStatusHistoryDto
                    {
                        Id = h.Id,
                        PreviousStatus = h.PreviousStatus,
                        NewStatus = h.NewStatus,
                        ChangedAt = h.ChangedAt,
                        ChangedBy = h.ChangedBy,
                        Comment = h.Comment
                    })
                    .ToList(),
                AllowedTransitions = _transitionValidator.GetAllowedTransitions(order.Status).ToList()
            };

            return dto;
        }
    }
}