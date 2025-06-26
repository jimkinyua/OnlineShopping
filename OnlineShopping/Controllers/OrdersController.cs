using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.DTOs;
using OnlineShopping.Models;
using OnlineShopping.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace OnlineShopping.Controllers
{
    /// <summary>
    /// Manages order operations including creation, retrieval, status updates, and discount calculations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
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

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="createOrderDto">Order creation data including customer ID and order items</param>
        /// <returns>Created order details with applied discounts</returns>
        /// <response code="200">Order successfully created</response>
        /// <response code="400">Invalid order data provided</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new order",
            Description = "Creates a new order for a customer with automatic discount calculation and application",
            OperationId = "CreateOrder",
            Tags = new[] { "Orders" }
        )]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Calculates order total with applicable discounts without creating the order
        /// </summary>
        /// <param name="createOrderDto">Order data for calculation</param>
        /// <returns>Estimated total and applicable promotions</returns>
        /// <response code="200">Calculation successful</response>
        /// <response code="400">Invalid calculation data</response>
        [HttpPost("calculate-total")]
        [SwaggerOperation(
            Summary = "Calculate order total",
            Description = "Previews the order total with all applicable discounts without creating an actual order",
            OperationId = "CalculateOrderTotal",
            Tags = new[] { "Orders" }
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Retrieves a specific order by ID
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Order details including items and discounts</returns>
        /// <response code="200">Order found and returned</response>
        /// <response code="404">Order not found</response>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get order by ID",
            Description = "Retrieves detailed information about a specific order including items and applied discounts",
            OperationId = "GetOrderById",
            Tags = new[] { "Orders" }
        )]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrder([FromRoute] int id)
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

        /// <summary>
        /// Retrieves all orders
        /// </summary>
        /// <returns>List of all orders with details</returns>
        /// <response code="200">Orders retrieved successfully</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all orders",
            Description = "Retrieves a list of all orders in the system with full details",
            OperationId = "GetAllOrders",
            Tags = new[] { "Orders" }
        )]
        [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Retrieves all orders for a specific customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>List of customer's orders</returns>
        /// <response code="200">Orders retrieved successfully</response>
        [HttpGet("customer/{customerId}")]
        [SwaggerOperation(
            Summary = "Get customer orders",
            Description = "Retrieves all orders placed by a specific customer",
            OperationId = "GetOrdersByCustomer",
            Tags = new[] { "Orders" }
        )]
        [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByCustomer([FromRoute] int customerId)
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

        /// <summary>
        /// Gets available promotions for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="orderAmount">Optional order amount to filter promotions</param>
        /// <returns>List of applicable promotions</returns>
        /// <response code="200">Promotions retrieved successfully</response>
        [HttpGet("customer/{customerId}/promotions")]
        [SwaggerOperation(
            Summary = "Get available promotions",
            Description = "Retrieves all promotions available to a specific customer based on their segment and order history",
            OperationId = "GetAvailablePromotions",
            Tags = new[] { "Orders", "Promotions" }
        )]
        [ProducesResponseType(typeof(List<PromotionRuleDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailablePromotions([FromRoute] int customerId, [FromQuery] decimal orderAmount = 0)
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

        /// <summary>
        /// Gets order statistics for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>Customer order statistics</returns>
        /// <response code="200">Statistics retrieved successfully</response>
        [HttpGet("customer/{customerId}/statistics")]
        [SwaggerOperation(
            Summary = "Get customer statistics",
            Description = "Retrieves order statistics for a customer including total spent, order count, and first-time status",
            OperationId = "GetCustomerOrderStatistics",
            Tags = new[] { "Orders", "Analytics" }
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomerOrderStatistics([FromRoute] int customerId)
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

        /// <summary>
        /// Updates the status of an order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="updateDto">Status update information</param>
        /// <returns>Updated order details</returns>
        /// <response code="200">Status updated successfully</response>
        /// <response code="400">Invalid status transition</response>
        /// <response code="404">Order not found</response>
        [HttpPut("{id}/status")]
        [SwaggerOperation(
            Summary = "Update order status",
            Description = "Updates the status of an order following valid transition rules",
            OperationId = "UpdateOrderStatus",
            Tags = new[] { "Orders" }
        )]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrderStatus([FromRoute] int id, [FromBody] UpdateOrderStatusDto updateDto)
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

        /// <summary>
        /// Retrieves orders by status
        /// </summary>
        /// <param name="status">Order status to filter by</param>
        /// <returns>List of orders with the specified status</returns>
        /// <response code="200">Orders retrieved successfully</response>
        [HttpGet("status/{status}")]
        [SwaggerOperation(
            Summary = "Get orders by status",
            Description = "Retrieves all orders with a specific status",
            OperationId = "GetOrdersByStatus",
            Tags = new[] { "Orders" }
        )]
        [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByStatus([FromRoute] OrderStatus status)
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

        /// <summary>
        /// Gets order statistics grouped by status
        /// </summary>
        /// <returns>Order count and value statistics by status</returns>
        /// <response code="200">Statistics retrieved successfully</response>
        [HttpGet("statistics")]
        [SwaggerOperation(
            Summary = "Get order statistics",
            Description = "Retrieves aggregate statistics about orders grouped by status",
            OperationId = "GetOrderStatistics",
            Tags = new[] { "Orders", "Analytics" }
        )]
        [ProducesResponseType(typeof(List<OrderStatusSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderStatistics()
        {
            var stats = await _orderStatusService.GetOrderStatusStatisticsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Gets orders that haven't been updated recently
        /// </summary>
        /// <param name="daysThreshold">Number of days to consider an order stale (default: 7)</param>
        /// <returns>List of stale orders</returns>
        /// <response code="200">Stale orders retrieved successfully</response>
        [HttpGet("stale")]
        [SwaggerOperation(
            Summary = "Get stale orders",
            Description = "Retrieves orders that haven't been updated within the specified number of days",
            OperationId = "GetStaleOrders",
            Tags = new[] { "Orders", "Analytics" }
        )]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStaleOrders([FromQuery] int daysThreshold = 7)
        {
            var orders = await _orderStatusService.GetStaleOrdersAsync(daysThreshold);
            return Ok(orders);
        }
    }
}