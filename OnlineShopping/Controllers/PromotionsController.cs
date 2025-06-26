using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.DTOs;
using OnlineShopping.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace OnlineShopping.Controllers
{
    /// <summary>
    /// Manages promotion rules and discount configurations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class PromotionsController : ControllerBase
    {
        private readonly OrderDbContext _context;

        public PromotionsController(OrderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all promotions
        /// </summary>
        /// <param name="activeOnly">Filter to show only active promotions</param>
        /// <returns>List of promotion rules</returns>
        /// <response code="200">Promotions retrieved successfully</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all promotions",
            Description = "Retrieves all promotion rules with optional filtering for active promotions only",
            OperationId = "GetAllPromotions",
            Tags = new[] { "Promotions" }
        )]
        [ProducesResponseType(typeof(List<PromotionRuleDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPromotions([FromQuery] bool? activeOnly = null)
        {
            var query = _context.PromotionRules.AsQueryable();

            if (activeOnly.HasValue && activeOnly.Value)
            {
                query = query.Where(p => p.IsActive &&
                    p.StartDate <= DateTime.UtcNow &&
                    (p.EndDate == null || p.EndDate >= DateTime.UtcNow));
            }

            var promotions = await query
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync();

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
        /// Retrieves a specific promotion by ID
        /// </summary>
        /// <param name="id">Promotion ID</param>
        /// <returns>Detailed promotion information</returns>
        /// <response code="200">Promotion found and returned</response>
        /// <response code="404">Promotion not found</response>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get promotion by ID",
            Description = "Retrieves detailed information about a specific promotion rule",
            OperationId = "GetPromotion",
            Tags = new[] { "Promotions" }
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPromotion([FromRoute] int id)
        {
            var promotion = await _context.PromotionRules.FindAsync(id);
            if (promotion == null)
                return NotFound();

            return Ok(new
            {
                promotion.Id,
                promotion.Name,
                promotion.Description,
                Type = promotion.Type.ToString(),
                Criteria = promotion.Criteria.ToString(),
                promotion.DiscountValue,
                promotion.CriteriaValue,
                promotion.MinimumOrderAmount,
                promotion.MinimumOrderCount,
                promotion.MinimumTotalSpent,
                promotion.BuyQuantity,
                promotion.GetQuantity,
                promotion.StartDate,
                promotion.EndDate,
                promotion.IsActive,
                promotion.Priority,
                promotion.IsCombinable,
                promotion.MaxUsesPerCustomer
            });
        }

        /// <summary>
        /// Creates a new promotion
        /// </summary>
        /// <param name="dto">Promotion creation data</param>
        /// <returns>Created promotion details</returns>
        /// <response code="201">Promotion successfully created</response>
        /// <response code="400">Invalid promotion data</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create new promotion",
            Description = "Creates a new promotion rule with specified criteria and discount configuration",
            OperationId = "CreatePromotion",
            Tags = new[] { "Promotions" }
        )]
        [ProducesResponseType(typeof(PromotionRule), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionDto dto)
        {
            var promotion = new PromotionRule
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                Criteria = dto.Criteria,
                DiscountValue = dto.DiscountValue,
                CriteriaValue = dto.CriteriaValue,
                MinimumOrderAmount = dto.MinimumOrderAmount,
                MinimumOrderCount = dto.MinimumOrderCount,
                MinimumTotalSpent = dto.MinimumTotalSpent,
                BuyQuantity = dto.BuyQuantity,
                GetQuantity = dto.GetQuantity,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                Priority = dto.Priority,
                IsCombinable = dto.IsCombinable,
                MaxUsesPerCustomer = dto.MaxUsesPerCustomer
            };

            _context.PromotionRules.Add(promotion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, promotion);
        }

        /// <summary>
        /// Updates an existing promotion
        /// </summary>
        /// <param name="id">Promotion ID</param>
        /// <param name="dto">Update data</param>
        /// <returns>Updated promotion details</returns>
        /// <response code="200">Promotion successfully updated</response>
        /// <response code="404">Promotion not found</response>
        [HttpPut("{id}")]
        [SwaggerOperation(
            Summary = "Update promotion",
            Description = "Updates properties of an existing promotion rule",
            OperationId = "UpdatePromotion",
            Tags = new[] { "Promotions" }
        )]
        [ProducesResponseType(typeof(PromotionRule), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePromotion([FromRoute] int id, [FromBody] UpdatePromotionDto dto)
        {
            var promotion = await _context.PromotionRules.FindAsync(id);
            if (promotion == null)
                return NotFound();

            promotion.Name = dto.Name ?? promotion.Name;
            promotion.Description = dto.Description ?? promotion.Description;
            promotion.IsActive = dto.IsActive ?? promotion.IsActive;
            promotion.EndDate = dto.EndDate;
            promotion.Priority = dto.Priority ?? promotion.Priority;
            promotion.IsCombinable = dto.IsCombinable ?? promotion.IsCombinable;
            promotion.MaxUsesPerCustomer = dto.MaxUsesPerCustomer;

            await _context.SaveChangesAsync();

            return Ok(promotion);
        }

        /// <summary>
        /// Deletes a promotion
        /// </summary>
        /// <param name="id">Promotion ID</param>
        /// <returns>Deletion confirmation</returns>
        /// <response code="200">Promotion successfully deleted or deactivated</response>
        /// <response code="404">Promotion not found</response>
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Delete promotion",
            Description = "Deletes a promotion (soft delete if used, hard delete if unused)",
            OperationId = "DeletePromotion",
            Tags = new[] { "Promotions" }
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePromotion([FromRoute] int id)
        {
            var promotion = await _context.PromotionRules.FindAsync(id);
            if (promotion == null)
                return NotFound();

            // Check if the promotion has been used
            var hasBeenUsed = await _context.AppliedDiscounts
                .AnyAsync(ad => ad.PromotionRuleId == id);

            if (hasBeenUsed)
            {
                // Soft delete - just deactivate it
                promotion.IsActive = false;
                promotion.EndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Promotion has been deactivated (soft delete) as it has been used in orders." });
            }
            else
            {
                // Hard delete - remove from database
                _context.PromotionRules.Remove(promotion);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Promotion has been permanently deleted." });
            }
        }

        /// <summary>
        /// Gets usage statistics for a promotion
        /// </summary>
        /// <param name="id">Promotion ID</param>
        /// <returns>Promotion usage statistics</returns>
        /// <response code="200">Usage statistics retrieved successfully</response>
        /// <response code="404">Promotion not found</response>
        [HttpGet("{id}/usage")]
        [SwaggerOperation(
            Summary = "Get promotion usage",
            Description = "Retrieves detailed usage statistics for a specific promotion including customer breakdown",
            OperationId = "GetPromotionUsage",
            Tags = new[] { "Promotions", "Analytics" }
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPromotionUsage([FromRoute] int id)
        {
            var promotion = await _context.PromotionRules.FindAsync(id);
            if (promotion == null)
                return NotFound();

            var usage = await _context.AppliedDiscounts
                .Include(ad => ad.Order)
                    .ThenInclude(o => o.Customer)
                .Where(ad => ad.PromotionRuleId == id)
                .GroupBy(ad => ad.Order.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    CustomerName = g.First().Order.Customer.Name,
                    UsageCount = g.Count(),
                    TotalDiscountAmount = g.Sum(ad => ad.DiscountAmount),
                    LastUsed = g.Max(ad => ad.AppliedAt)
                })
                .ToListAsync();

            var summary = new
            {
                PromotionName = promotion.Name,
                TotalUsages = usage.Sum(u => u.UsageCount),
                UniqueCustomers = usage.Count,
                TotalDiscountGiven = usage.Sum(u => u.TotalDiscountAmount),
                CustomerUsage = usage
            };

            return Ok(summary);
        }
    }

    /// <summary>
    /// Data transfer object for creating a new promotion
    /// </summary>
    [SwaggerSchema("Promotion creation parameters")]
    public class CreatePromotionDto
    {
        /// <summary>
        /// Promotion name
        /// </summary>
        [SwaggerSchema("Name of the promotion", Required = new[] { "Name" })]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Promotion description
        /// </summary>
        [SwaggerSchema("Detailed description of the promotion")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type of promotion (Percentage, FixedAmount, etc.)
        /// </summary>
        [SwaggerSchema("Promotion discount type", Required = new[] { "Type" })]
        public PromotionType Type { get; set; }

        /// <summary>
        /// Promotion criteria (OrderAmount, CustomerSegment, etc.)
        /// </summary>
        [SwaggerSchema("Criteria for promotion eligibility", Required = new[] { "Criteria" })]
        public PromotionCriteria Criteria { get; set; }

        /// <summary>
        /// Discount value (percentage or fixed amount)
        /// </summary>
        [SwaggerSchema("Discount value - percentage (0-100) or fixed amount", Required = new[] { "DiscountValue" })]
        public decimal DiscountValue { get; set; }

        /// <summary>
        /// Criteria-specific value (e.g., customer segment name)
        /// </summary>
        [SwaggerSchema("Additional criteria value (e.g., 'VIP' for CustomerSegment criteria)")]
        public string? CriteriaValue { get; set; }

        /// <summary>
        /// Minimum order amount required
        /// </summary>
        [SwaggerSchema("Minimum order amount for eligibility")]
        public decimal? MinimumOrderAmount { get; set; }

        /// <summary>
        /// Minimum order count required
        /// </summary>
        [SwaggerSchema("Minimum number of previous orders required")]
        public int? MinimumOrderCount { get; set; }

        /// <summary>
        /// Minimum total spent required
        /// </summary>
        [SwaggerSchema("Minimum historical spending required")]
        public decimal? MinimumTotalSpent { get; set; }

        /// <summary>
        /// Buy quantity for BOGO promotions
        /// </summary>
        [SwaggerSchema("Number of items to buy (for BOGO)")]
        public int? BuyQuantity { get; set; }

        /// <summary>
        /// Get quantity for BOGO promotions
        /// </summary>
        [SwaggerSchema("Number of free items (for BOGO)")]
        public int? GetQuantity { get; set; }

        /// <summary>
        /// Promotion start date
        /// </summary>
        [SwaggerSchema("When the promotion becomes active")]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Promotion end date
        /// </summary>
        [SwaggerSchema("When the promotion expires")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Whether the promotion is active
        /// </summary>
        [SwaggerSchema("Promotion active status")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Promotion priority (lower number = higher priority)
        /// </summary>
        [SwaggerSchema("Priority order for applying promotions")]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Whether promotion can be combined with others
        /// </summary>
        [SwaggerSchema("Can be combined with other promotions")]
        public bool IsCombinable { get; set; } = true;

        /// <summary>
        /// Maximum uses per customer
        /// </summary>
        [SwaggerSchema("Maximum times a customer can use this promotion")]
        public int? MaxUsesPerCustomer { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating a promotion
    /// </summary>
    [SwaggerSchema("Promotion update parameters")]
    public class UpdatePromotionDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Priority { get; set; }
        public bool? IsCombinable { get; set; }
        public int? MaxUsesPerCustomer { get; set; }
    }
}