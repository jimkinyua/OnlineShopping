using System;

namespace OnlineShopping.Models
{
    public enum PromotionType
    {
        PercentageDiscount,
        FixedAmountDiscount,
        BuyXGetY,
        FreeShipping,
        MinimumPurchaseDiscount
    }

    public class Promotion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PromotionType Type { get; set; }
        public decimal DiscountValue { get; set; } // Percentage or fixed amount
        public decimal? MinimumPurchaseAmount { get; set; }
        public CustomerSegment? TargetSegment { get; set; } // null means all segments
        public int? MinimumOrderCount { get; set; } // Minimum number of previous orders
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; } // Higher priority promotions are applied first
    }
}