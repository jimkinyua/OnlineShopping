using Microsoft.AspNetCore.Mvc;
using OnlineShopping.DTOs;
using OnlineShopping.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace OnlineShopping.Controllers
{
    /// <summary>
    /// Manages customer operations including creation, retrieval, and customer information management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerManagement _customerManagement;

        public CustomersController(ICustomerManagement customerManagement)
        {
            _customerManagement = customerManagement;
        }

        /// <summary>
        /// Creates a new customer
        /// </summary>
        /// <param name="createCustomerDto">Customer creation data</param>
        /// <returns>Newly created customer information</returns>
        /// <response code="201">Customer successfully created</response>
        /// <response code="400">Invalid customer data provided</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new customer",
            Description = "Creates a new customer with the provided information and assigns them to a customer segment",
            OperationId = "CreateCustomer",
            Tags = new[] { "Customers" }
        )]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Retrieves a specific customer by ID
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <returns>Customer information</returns>
        /// <response code="200">Customer found and returned</response>
        /// <response code="404">Customer not found</response>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get customer by ID",
            Description = "Retrieves detailed information about a specific customer",
            OperationId = "GetCustomerById",
            Tags = new[] { "Customers" }
        )]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomerById([FromRoute] int id)
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

        /// <summary>
        /// Retrieves all customers
        /// </summary>
        /// <returns>List of all customers</returns>
        /// <response code="200">List of customers returned successfully</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all customers",
            Description = "Retrieves a list of all customers in the system",
            OperationId = "GetAllCustomers",
            Tags = new[] { "Customers" }
        )]
        [ProducesResponseType(typeof(List<CustomerResponseDto>), StatusCodes.Status200OK)]
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