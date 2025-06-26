using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.Models;
using System.Text.Json;

namespace OnlineShopping.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly OrderDbContext _context;

        public DiscountService(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<List<PromotionRule>> GetApplicablePromotionsAsync(int customerId, decimal orderSubTotal)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return new List<PromotionRule>();

            var activePromotions = await _context.PromotionRules
                .Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow &&
                           (p.EndDate == null || p.EndDate >= DateTime.UtcNow))
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Id)
                .ToListAsync();

            var applicablePromotions = new List<PromotionRule>();

            foreach (var promotion in activePromotions)
            {
                if (await IsPromotionApplicableAsync(customer, promotion, orderSubTotal))
                {
                    applicablePromotions.Add(promotion);
                }
            }

            return applicablePromotions;
        }

        public async Task<(decimal totalDiscount, List<AppliedDiscount> appliedDiscounts)> CalculateDiscountsAsync(
            int customerId,
            decimal orderSubTotal,
            List<OrderItem> orderItems)
        {
            var applicablePromotions = await GetApplicablePromotionsAsync(customerId, orderSubTotal);
            var appliedDiscounts = new List<AppliedDiscount>();
            decimal totalDiscount = 0;

            // Group promotions by combinability
            var nonCombinablePromotions = applicablePromotions.Where(p => !p.IsCombinable).ToList();
            var combinablePromotions = applicablePromotions.Where(p => p.IsCombinable).ToList();

            // If there are non-combinable promotions, choose the best one
            if (nonCombinablePromotions.Any())
            {
                var bestPromotion = await GetBestPromotionAsync(nonCombinablePromotions, orderSubTotal, orderItems);
                if (bestPromotion != null)
                {
                    var discount = CalculatePromotionDiscount(bestPromotion, orderSubTotal, orderItems);
                    // Always create the applied discount record, even if discount is 0
                    appliedDiscounts.Add(CreateAppliedDiscount(bestPromotion, discount));
                    totalDiscount += discount;
                }
            }
            else
            {
                // Apply all combinable promotions
                foreach (var promotion in combinablePromotions)
                {
                    var discount = CalculatePromotionDiscount(promotion, orderSubTotal - totalDiscount, orderItems);
                    // Always create the applied discount record, even if discount is 0
                    appliedDiscounts.Add(CreateAppliedDiscount(promotion, discount));
                    totalDiscount += discount;
                }
            }

            return (totalDiscount, appliedDiscounts);
        }

        private async Task<bool> IsPromotionApplicableAsync(Customer customer, PromotionRule promotion, decimal orderSubTotal)
        {
            // Check maximum uses per customer
            if (promotion.MaxUsesPerCustomer.HasValue)
            {
                var usageCount = await GetPromotionUsageCountAsync(customer.Id, promotion.Id);
                if (usageCount >= promotion.MaxUsesPerCustomer.Value)
                {
                    return false;
                }
            }

            // Check minimum order amount
            if (promotion.MinimumOrderAmount.HasValue && orderSubTotal < promotion.MinimumOrderAmount.Value)
            {
                return false;
            }

            // Check criteria
            switch (promotion.Criteria)
            {
                case PromotionCriteria.CustomerSegment:
                    return promotion.CriteriaValue == customer.Segment.ToString();

                case PromotionCriteria.OrderCount:
                    if (promotion.MinimumOrderCount.HasValue)
                    {
                        var orderCount = await GetCustomerOrderCountAsync(customer.Id);
                        return orderCount >= promotion.MinimumOrderCount.Value;
                    }
                    return true; // No order count requirement

                case PromotionCriteria.TotalSpent:
                    if (promotion.MinimumTotalSpent.HasValue)
                    {
                        var totalSpent = await GetCustomerTotalSpentAsync(customer.Id);
                        return totalSpent >= promotion.MinimumTotalSpent.Value;
                    }
                    return true; // No total spent requirement

                case PromotionCriteria.FirstTimeCustomer:
                    return await IsFirstTimeCustomerAsync(customer.Id);

                case PromotionCriteria.OrderAmount:
                    return true; // Already checked with MinimumOrderAmount

                case PromotionCriteria.SpecificProduct:
                    // This would need to check if specific products are in the order
                    // For now, returning true as we don't have the order items context here
                    return true;

                default:
                    return false; // Unknown criteria
            }
        }

        private decimal CalculatePromotionDiscount(PromotionRule promotion, decimal baseAmount, List<OrderItem> orderItems)
        {
            // For percentage discounts, even if baseAmount is 0, the discount would be 0
            // For fixed discounts, we shouldn't apply them if baseAmount is 0

            switch (promotion.Type)
            {
                case PromotionType.PercentageDiscount:
                    return Math.Round(baseAmount * (promotion.DiscountValue / 100), 2);

                case PromotionType.FixedAmountDiscount:
                    // Don't apply fixed discount if base amount is 0 or negative
                    if (baseAmount <= 0)
                        return 0;
                    return Math.Min(promotion.DiscountValue, baseAmount);

                case PromotionType.FreeShipping:
                    // This would be handled separately in the order calculation
                    return 0;

                case PromotionType.BuyXGetY:
                    // This would need more complex logic based on order items
                    // For now, returning 0
                    return 0;

                default:
                    return 0;
            }
        }

        private async Task<PromotionRule?> GetBestPromotionAsync(
            List<PromotionRule> promotions,
            decimal orderSubTotal,
            List<OrderItem> orderItems)
        {
            decimal maxDiscount = 0;
            PromotionRule? bestPromotion = null;

            foreach (var promotion in promotions)
            {
                var discount = CalculatePromotionDiscount(promotion, orderSubTotal, orderItems);
                if (discount > maxDiscount)
                {
                    maxDiscount = discount;
                    bestPromotion = promotion;
                }
            }

            return bestPromotion;
        }

        private AppliedDiscount CreateAppliedDiscount(PromotionRule promotion, decimal discountAmount)
        {
            return new AppliedDiscount
            {
                PromotionRuleId = promotion.Id,
                DiscountAmount = discountAmount,
                PromotionSnapshot = JsonSerializer.Serialize(new
                {
                    promotion.Name,
                    promotion.Description,
                    Type = promotion.Type.ToString(),
                    promotion.DiscountValue
                }),
                AppliedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> IsPromotionValidForCustomerAsync(int customerId, int promotionRuleId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            var promotion = await _context.PromotionRules.FindAsync(promotionRuleId);

            if (customer == null || promotion == null || !promotion.IsActive)
            {
                return false;
            }

            return await IsPromotionApplicableAsync(customer, promotion, 0);
        }

        public async Task<int> GetCustomerOrderCountAsync(int customerId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled)
                .CountAsync();
        }

        public async Task<decimal> GetCustomerTotalSpentAsync(int customerId)
        {
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId &&
                           o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            return orders.Sum(o => o.TotalAmount);
        }

        public async Task<bool> IsFirstTimeCustomerAsync(int customerId)
        {
            return !await _context.Orders
                .AnyAsync(o => o.CustomerId == customerId &&
                              o.Status != OrderStatus.Cancelled);
        }

        public async Task<int> GetPromotionUsageCountAsync(int customerId, int promotionRuleId)
        {
            return await _context.AppliedDiscounts
                .Include(ad => ad.Order)
                .Where(ad => ad.Order.CustomerId == customerId &&
                            ad.PromotionRuleId == promotionRuleId)
                .CountAsync();
        }
    }
}