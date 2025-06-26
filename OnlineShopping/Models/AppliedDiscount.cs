namespace OnlineShopping.Models
{
        public class AppliedDiscount
        {
                public int Id { get; set; }

                public int OrderId { get; set; }
                public Order Order { get; set; } = null!;

                public int PromotionRuleId { get; set; }
                public PromotionRule PromotionRule { get; set; } = null!;

                public decimal DiscountAmount { get; set; }

                public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

                // Store the snapshot of the promotion details at the time of application
                public string PromotionSnapshot { get; set; } = string.Empty;
        }
}