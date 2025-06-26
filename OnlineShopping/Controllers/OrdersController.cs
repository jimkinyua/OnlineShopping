using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.DTOs;
using OnlineShopping.Services;

namespace OnlineShopping.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderManagement _orderManagement;
        private readonly OrderDbContext _context;

        public OrdersController(IOrderManagement orderManagement, OrderDbContext context)
        {
            _orderManagement = orderManagement;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (createOrderDto.CustomerId <= 0)
            {
                return BadRequest("Invalid customer ID.");
            }

            if (createOrderDto.SubtotalAmount <= 0)
            {
                return BadRequest("Subtotal amount must be greater than zero.");
            }

            var order = await _orderManagement.CreateOrderAsync(createOrderDto.CustomerId,createOrderDto.SubtotalAmount);

            var orderResponse = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name ?? "",
                CustomerSegment = order.Customer?.Segment ?? Models.CustomerSegment.Regular,
                OrderDate = order.OrderDate,
                Status = order.Status,
                SubtotalAmount = order.SubtotalAmount,
                TotalDiscount = order.TotalDiscount,
                TotalAmount = order.TotalAmount,
                AppliedDiscounts = order.AppliedDiscounts.Select(d => new AppliedDiscountDto
                {
                    PromotionName = d.Promotion?.Name ?? "",
                    PromotionDescription = d.Promotion?.Description ?? "",
                    DiscountAmount = d.DiscountAmount
                }).ToList()
            };

            return Ok(orderResponse);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.AppliedDiscounts)
                    .ThenInclude(d => d.Promotion)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var orderResponse = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name ?? "",
                CustomerSegment = order.Customer?.Segment ?? Models.CustomerSegment.Regular,
                OrderDate = order.OrderDate,
                Status = order.Status,
                SubtotalAmount = order.SubtotalAmount,
                TotalDiscount = order.TotalDiscount,
                TotalAmount = order.TotalAmount,
                AppliedDiscounts = order.AppliedDiscounts.Select(d => new AppliedDiscountDto
                {
                    PromotionName = d.Promotion?.Name ?? "",
                    PromotionDescription = d.Promotion?.Description ?? "",
                    DiscountAmount = d.DiscountAmount
                }).ToList()
            };

            return Ok(orderResponse);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetCustomerOrders(int customerId)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.AppliedDiscounts)
                    .ThenInclude(d => d.Promotion)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderResponses = orders.Select(order => new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name ?? "",
                CustomerSegment = order.Customer?.Segment ?? Models.CustomerSegment.Regular,
                OrderDate = order.OrderDate,
                Status = order.Status,
                SubtotalAmount = order.SubtotalAmount,
                TotalDiscount = order.TotalDiscount,
                TotalAmount = order.TotalAmount,
                AppliedDiscounts = order.AppliedDiscounts.Select(d => new AppliedDiscountDto
                {
                    PromotionName = d.Promotion?.Name ?? "",
                    PromotionDescription = d.Promotion?.Description ?? "",
                    DiscountAmount = d.DiscountAmount
                }).ToList()
            }).ToList();

            return Ok(orderResponses);
        }
    }
}