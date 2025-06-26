using Microsoft.AspNetCore.Mvc;
using OnlineShopping.DTOs;
using OnlineShopping.Models;
using OnlineShopping.Services;

namespace OnlineShopping.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderManagement _orderManagement;
        private readonly IOrderStatusService _orderStatusService;

        public OrdersController(IOrderManagement orderManagement, IOrderStatusService orderStatusService)
        {
            _orderManagement = orderManagement;
            _orderStatusService = orderStatusService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            var order = await _orderManagement.CreateOrderAsync(
                createOrderDto.CustomerId,
                createOrderDto.TotalAmount);

            return Ok(order);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _orderStatusService.GetOrderWithStatusHistoryAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderManagement.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetOrdersByCustomer(int customerId)
        {
            var orders = await _orderManagement.GetOrdersByCustomerIdAsync(customerId);
            return Ok(orders);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            try
            {
                var order = await _orderStatusService.UpdateOrderStatusAsync(id, updateDto);
                if (order == null)
                    return NotFound();

                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetOrdersByStatus(OrderStatus status)
        {
            var orders = await _orderStatusService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetOrderStatistics()
        {
            var stats = await _orderStatusService.GetOrderStatusStatisticsAsync();
            return Ok(stats);
        }

        [HttpGet("stale")]
        public async Task<IActionResult> GetStaleOrders([FromQuery] int daysThreshold = 7)
        {
            var orders = await _orderStatusService.GetStaleOrdersAsync(daysThreshold);
            return Ok(orders);
        }
    }
}