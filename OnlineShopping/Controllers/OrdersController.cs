using Microsoft.AspNetCore.Mvc;
using OnlineShopping.DTOs;
using OnlineShopping.Services;

namespace OnlineShopping.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderManagement _orderManagement;

        public OrdersController(IOrderManagement orderManagement)
        {
            _orderManagement = orderManagement;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            var order = await _orderManagement.CreateOrderAsync(
                createOrderDto.CustomerId,
                createOrderDto.TotalAmount);

            return Ok(order);
        }
    }
}