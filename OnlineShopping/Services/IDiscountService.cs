using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public interface IDiscountService
    {
        Task<List<PromotionRule>> GetApplicablePromotionsAsync(int customerId, decimal orderSubTotal);
        Task<(decimal totalDiscount, List<AppliedDiscount> appliedDiscounts)> CalculateDiscountsAsync(
            int customerId,
            decimal orderSubTotal,
            List<OrderItem> orderItems);
        Task<bool> IsPromotionValidForCustomerAsync(int customerId, int promotionRuleId);
        Task<int> GetCustomerOrderCountAsync(int customerId);
        Task<decimal> GetCustomerTotalSpentAsync(int customerId);
        Task<bool> IsFirstTimeCustomerAsync(int customerId);
        Task<int> GetPromotionUsageCountAsync(int customerId, int promotionRuleId);
    }
}