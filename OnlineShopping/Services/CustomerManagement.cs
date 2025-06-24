using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.DTOs;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public class CustomerManagement : ICustomerManagement
    {
        private readonly OrderDbContext _context;

        public CustomerManagement(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Customer> CreateCustomerAsync(CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                Name = dto.Name,
                Email = dto.Email,
                Segment = dto.Segment
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .OrderBy(c => c.Id)
                .ToListAsync();
        }
    }
}