using OnlineShopping.DTOs;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public interface IOrderManagement
    {
        Task<Order> CreateOrderAsync(CreateOrderDto orderDto);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<OrderResponseDto> GetOrderDetailsAsync(int orderId);
        Task<List<PromotionRule>> GetAvailablePromotionsAsync(int customerId, decimal orderAmount);
        Task<decimal> CalculateOrderTotalAsync(int customerId, List<OrderItem> items);
    }
}