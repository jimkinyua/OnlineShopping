using System.ComponentModel.DataAnnotations;

namespace OnlineShopping.Models
{
    public enum PromotionCriteria
    {
        CustomerSegment,
        OrderCount,
        TotalSpent,
        SpecificProduct,
        OrderAmount,
        FirstTimeCustomer
    }

    public class PromotionRule
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public PromotionType Type { get; set; }

        public PromotionCriteria Criteria { get; set; }

        // For percentage or fixed amount discounts
        public decimal DiscountValue { get; set; }

        // For criteria evaluation
        public string? CriteriaValue { get; set; } // Can store segment name, product ID, etc.
        public decimal? MinimumOrderAmount { get; set; }
        public int? MinimumOrderCount { get; set; }
        public decimal? MinimumTotalSpent { get; set; }

        // For Buy X Get Y promotions
        public int? BuyQuantity { get; set; }
        public int? GetQuantity { get; set; }

        // Validity period
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Priority for applying multiple promotions
        public int Priority { get; set; } = 0;

        // Can this promotion be combined with others?
        public bool IsCombinable { get; set; } = true;

        // Maximum uses per customer (null = unlimited)
        public int? MaxUsesPerCustomer { get; set; }

        // Navigation property
        public ICollection<AppliedDiscount> AppliedDiscounts { get; set; } = new List<AppliedDiscount>();
    }
}