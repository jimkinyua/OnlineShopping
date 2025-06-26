using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.DTOs;
using OnlineShopping.Models;

namespace OnlineShopping.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionsController : ControllerBase
    {
        private readonly OrderDbContext _context;

        public PromotionsController(OrderDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotion(int id)
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

        [HttpPost]
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] UpdatePromotionDto dto)
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
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

        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetPromotionUsage(int id)
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

    public class CreatePromotionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PromotionType Type { get; set; }
        public PromotionCriteria Criteria { get; set; }
        public decimal DiscountValue { get; set; }
        public string? CriteriaValue { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public int? MinimumOrderCount { get; set; }
        public decimal? MinimumTotalSpent { get; set; }
        public int? BuyQuantity { get; set; }
        public int? GetQuantity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0;
        public bool IsCombinable { get; set; } = true;
        public int? MaxUsesPerCustomer { get; set; }
    }

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