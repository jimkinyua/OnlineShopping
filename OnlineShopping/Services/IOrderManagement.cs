using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public interface IOrderManagement
    {
        Task<Order> CreateOrderAsync(int customerId, decimal totalAmount);
    }
}