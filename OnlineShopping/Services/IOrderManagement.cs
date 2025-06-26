using OnlineShopping.DTOs;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public interface IOrderManagement
    {
        Task<Order> CreateOrderAsync(int customerId, decimal totalAmount);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task<List<Order>> GetAllOrdersAsync();
    }
}