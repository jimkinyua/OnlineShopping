using System.ComponentModel;

namespace OnlineShopping.Models
{
    /// <summary>
    /// Order status values
    /// </summary>
    public enum OrderStatusDocumented
    {
        /// <summary>
        /// Order has been created but not yet processed
        /// </summary>
        [Description("Order has been created but not yet processed")]
        Pending = 0,

        /// <summary>
        /// Order is being processed and prepared for shipment
        /// </summary>
        [Description("Order is being processed and prepared for shipment")]
        Processing = 1,

        /// <summary>
        /// Order has been shipped to the customer
        /// </summary>
        [Description("Order has been shipped to the customer")]
        Shipped = 2,

        /// <summary>
        /// Order has been delivered to the customer
        /// </summary>
        [Description("Order has been delivered to the customer")]
        Delivered = 3,

        /// <summary>
        /// Order has been cancelled
        /// </summary>
        [Description("Order has been cancelled")]
        Cancelled = 4,

        /// <summary>
        /// Order has been refunded
        /// </summary>
        [Description("Order has been refunded")]
        Refunded = 5
    }

    /// <summary>
    /// Customer segment classifications
    /// </summary>
    public enum CustomerSegmentDocumented
    {
        /// <summary>
        /// Regular customer - default segment
        /// </summary>
        [Description("Regular customer - default segment")]
        Regular = 0,

        /// <summary>
        /// Bronze tier customer
        /// </summary>
        [Description("Bronze tier customer")]
        Bronze = 1,

        /// <summary>
        /// Silver tier customer
        /// </summary>
        [Description("Silver tier customer")]
        Silver = 2,

        /// <summary>
        /// Gold tier customer
        /// </summary>
        [Description("Gold tier customer")]
        Gold = 3,

        /// <summary>
        /// VIP customer - highest tier
        /// </summary>
        [Description("VIP customer - highest tier")]
        VIP = 4
    }

    /// <summary>
    /// Promotion discount types
    /// </summary>
    public enum PromotionTypeDocumented
    {
        /// <summary>
        /// Percentage-based discount (e.g., 20% off)
        /// </summary>
        [Description("Percentage-based discount")]
        Percentage = 0,

        /// <summary>
        /// Fixed amount discount (e.g., $10 off)
        /// </summary>
        [Description("Fixed amount discount")]
        FixedAmount = 1,

        /// <summary>
        /// Buy One Get One free promotion
        /// </summary>
        [Description("Buy One Get One free")]
        BOGO = 2,

        /// <summary>
        /// Tiered discount based on quantity or amount
        /// </summary>
        [Description("Tiered discount")]
        Tiered = 3
    }

    /// <summary>
    /// Criteria for promotion eligibility
    /// </summary>
    public enum PromotionCriteriaDocumented
    {
        /// <summary>
        /// Based on order amount
        /// </summary>
        [Description("Based on order amount")]
        OrderAmount = 0,

        /// <summary>
        /// Based on customer segment
        /// </summary>
        [Description("Based on customer segment")]
        CustomerSegment = 1,

        /// <summary>
        /// For first-time customers only
        /// </summary>
        [Description("For first-time customers only")]
        FirstTimeCustomer = 2,

        /// <summary>
        /// Based on number of previous orders
        /// </summary>
        [Description("Based on order count")]
        OrderCount = 3,

        /// <summary>
        /// Based on total amount spent historically
        /// </summary>
        [Description("Based on total spent")]
        TotalSpent = 4,

        /// <summary>
        /// For specific products only
        /// </summary>
        [Description("For specific products")]
        SpecificProducts = 5,

        /// <summary>
        /// No specific criteria - applies to all
        /// </summary>
        [Description("No criteria - applies to all")]
        None = 6
    }
}