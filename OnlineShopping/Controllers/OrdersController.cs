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
        private readonly IDiscountService _discountService;

        public OrdersController(
            IOrderManagement orderManagement,
            IOrderStatusService orderStatusService,
            IDiscountService discountService)
        {
            _orderManagement = orderManagement;
            _orderStatusService = orderStatusService;
            _discountService = discountService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                var order = await _orderManagement.CreateOrderAsync(createOrderDto);
                var orderDetails = await _orderManagement.GetOrderDetailsAsync(order.Id);
                return Ok(orderDetails);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("calculate-total")]
        public async Task<IActionResult> CalculateOrderTotal([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                // Calculate subtotal
                decimal subTotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in createOrderDto.Items)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    };
                    orderItems.Add(orderItem);
                }

                var total = await _orderManagement.CalculateOrderTotalAsync(createOrderDto.CustomerId, orderItems);

                // Get applicable promotions
                var promotions = await _orderManagement.GetAvailablePromotionsAsync(createOrderDto.CustomerId, subTotal);

                return Ok(new
                {
                    estimatedTotal = total,
                    applicablePromotions = promotions.Select(p => new PromotionRuleDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Type = p.Type.ToString(),
                        DiscountValue = p.DiscountValue,
                        MinimumOrderAmount = p.MinimumOrderAmount,
                        IsActive = p.IsActive,
                        EndDate = p.EndDate
                    })
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var orderDetails = await _orderManagement.GetOrderDetailsAsync(id);
                return Ok(orderDetails);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderManagement.GetAllOrdersAsync();
            var orderDtos = new List<OrderResponseDto>();

            foreach (var order in orders)
            {
                var orderDto = await _orderManagement.GetOrderDetailsAsync(order.Id);
                orderDtos.Add(orderDto);
            }

            return Ok(orderDtos);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetOrdersByCustomer(int customerId)
        {
            var orders = await _orderManagement.GetOrdersByCustomerIdAsync(customerId);
            var orderDtos = new List<OrderResponseDto>();

            foreach (var order in orders)
            {
                var orderDto = await _orderManagement.GetOrderDetailsAsync(order.Id);
                orderDtos.Add(orderDto);
            }

            return Ok(orderDtos);
        }

        [HttpGet("customer/{customerId}/promotions")]
        public async Task<IActionResult> GetAvailablePromotions(int customerId, [FromQuery] decimal orderAmount = 0)
        {
            var promotions = await _orderManagement.GetAvailablePromotionsAsync(customerId, orderAmount);

            var promotionDtos = promotions.Select(p => new PromotionRuleDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Type = p.Type.ToString(),
                DiscountValue = p.DiscountValue,
                MinimumOrderAmount = p.MinimumOrderAmount,
                IsActive = p.IsActive,
                EndDate = p.EndDate
            });

            return Ok(promotionDtos);
        }

        [HttpGet("customer/{customerId}/statistics")]
        public async Task<IActionResult> GetCustomerOrderStatistics(int customerId)
        {
            var orderCount = await _discountService.GetCustomerOrderCountAsync(customerId);
            var totalSpent = await _discountService.GetCustomerTotalSpentAsync(customerId);
            var isFirstTime = await _discountService.IsFirstTimeCustomerAsync(customerId);

            return Ok(new
            {
                customerId,
                orderCount,
                totalSpent,
                isFirstTimeCustomer = isFirstTime,
                averageOrderValue = orderCount > 0 ? totalSpent / orderCount : 0
            });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            try
            {
                var order = await _orderStatusService.UpdateOrderStatusAsync(id, updateDto);
                if (order == null)
                    return NotFound();

                var orderDetails = await _orderManagement.GetOrderDetailsAsync(order.Id);
                return Ok(orderDetails);
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
            var orderDtos = new List<OrderResponseDto>();

            foreach (var order in orders)
            {
                var orderDto = await _orderManagement.GetOrderDetailsAsync(order.OrderId);
                orderDtos.Add(orderDto);
            }

            return Ok(orderDtos);
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