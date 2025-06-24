using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public interface IDiscountService
    {
        Task<List<AppliedDiscount>> CalculateDiscountsAsync(int customerId, decimal subtotalAmount);
        Task<decimal> GetTotalDiscountAmountAsync(List<AppliedDiscount> appliedDiscounts);
    }
}