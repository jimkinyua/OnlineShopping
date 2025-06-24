using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly OrderDbContext _context;

        public DiscountService(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<List<AppliedDiscount>> CalculateDiscountsAsync(int customerId, decimal subtotalAmount)
        {
            var appliedDiscounts = new List<AppliedDiscount>();

            // Get customer with their segment
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return appliedDiscounts;
            }

            // Get customer's order history count
            var orderCount = await _context.Orders
                .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled)
                .CountAsync();

            // Get all active promotions
            var now = DateTime.UtcNow;
            var activePromotions = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .OrderByDescending(p => p.Priority)
                .ToListAsync();

            decimal remainingAmount = subtotalAmount;

            foreach (var promotion in activePromotions)
            {
                // Check if promotion applies to this customer
                if (!IsPromotionApplicable(promotion, customer, orderCount, subtotalAmount))
                {
                    continue;
                }

                decimal discountAmount = CalculateDiscountAmount(promotion, remainingAmount);

                if (discountAmount > 0)
                {
                    appliedDiscounts.Add(new AppliedDiscount
                    {
                        PromotionId = promotion.Id,
                        Promotion = promotion,
                        DiscountAmount = discountAmount,
                        AppliedAt = DateTime.UtcNow
                    });

                    // For cumulative discounts, reduce the remaining amount
                    remainingAmount -= discountAmount;
                }
            }

            return appliedDiscounts;
        }

        public Task<decimal> GetTotalDiscountAmountAsync(List<AppliedDiscount> appliedDiscounts)
        {
            var total = appliedDiscounts.Sum(d => d.DiscountAmount);
            return Task.FromResult(total);
        }

        private bool IsPromotionApplicable(Promotion promotion, Customer customer, int orderCount, decimal subtotalAmount)
        {
            // Check customer segment
            if (promotion.TargetSegment.HasValue && promotion.TargetSegment.Value != customer.Segment)
            {
                return false;
            }

            // Check minimum order count
            if (promotion.MinimumOrderCount.HasValue && orderCount < promotion.MinimumOrderCount.Value)
            {
                return false;
            }

            // Check minimum purchase amount
            if (promotion.MinimumPurchaseAmount.HasValue && subtotalAmount < promotion.MinimumPurchaseAmount.Value)
            {
                return false;
            }

            return true;
        }

        private decimal CalculateDiscountAmount(Promotion promotion, decimal amount)
        {
            switch (promotion.Type)
            {
                case PromotionType.PercentageDiscount:
                    return Math.Round(amount * (promotion.DiscountValue / 100), 2);

                case PromotionType.FixedAmountDiscount:
                    // Fixed discount cannot exceed the amount
                    return Math.Min(promotion.DiscountValue, amount);

                case PromotionType.MinimumPurchaseDiscount:
                    // Similar to percentage but only applies if minimum is met
                    return Math.Round(amount * (promotion.DiscountValue / 100), 2);

                default:
                    return 0;
            }
        }
    }
}