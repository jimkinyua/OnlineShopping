using Microsoft.AspNetCore.Mvc;
using OnlineShopping.DTOs;
using OnlineShopping.Services;

namespace OnlineShopping.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerManagement _customerManagement;

        public CustomersController(ICustomerManagement customerManagement)
        {
            _customerManagement = customerManagement;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto createCustomerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _customerManagement.CreateCustomerAsync(createCustomerDto);

            var response = new CustomerResponseDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Segment = customer.Segment,
                CreatedAt = customer.CreatedAt
            };

            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _customerManagement.GetCustomerByIdAsync(id);

            if (customer == null)
            {
                return NotFound($"Customer with ID {id} not found");
            }

            var response = new CustomerResponseDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Segment = customer.Segment,
                CreatedAt = customer.CreatedAt
            };

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCustomers()
        {
            var customers = await _customerManagement.GetAllCustomersAsync();

            var response = customers.Select(c => new CustomerResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Segment = c.Segment,
                CreatedAt = c.CreatedAt
            }).ToList();

            return Ok(response);
        }
    }
}