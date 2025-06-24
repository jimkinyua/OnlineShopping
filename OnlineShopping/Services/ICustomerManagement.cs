using OnlineShopping.DTOs;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public interface ICustomerManagement
    {
        Task<Customer> CreateCustomerAsync(CreateCustomerDto dto);
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<List<Customer>> GetAllCustomersAsync();
    }
}