namespace OnlineShopping.DTOs
{
    public class AppliedDiscountDto
    {
        public int Id { get; set; }
        public string PromotionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public DateTime AppliedAt { get; set; }
    }

    public class PromotionRuleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class OrderDiscountSummaryDto
    {
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal FinalTotal { get; set; }
        public List<AppliedDiscountDto> AppliedDiscounts { get; set; } = new List<AppliedDiscountDto>();
    }
}