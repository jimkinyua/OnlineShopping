using Microsoft.EntityFrameworkCore;
using Moq;
using OnlineShopping.Data;
using OnlineShopping.Models;
using OnlineShopping.Services;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace OnlineShopping.Tests.Services
{
    public class DiscountServiceTests : IDisposable
    {
        private readonly OrderDbContext _context;
        private readonly DiscountService _discountService;

        public DiscountServiceTests()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new OrderDbContext(options);
            _discountService = new DiscountService(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test customers
            var regularCustomer = new Customer
            {
                Id = 1,
                Name = "Regular Customer",
                Email = "regular@test.com",
                Segment = CustomerSegment.Regular,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            };

            var vipCustomer = new Customer
            {
                Id = 2,
                Name = "VIP Customer",
                Email = "vip@test.com",
                Segment = CustomerSegment.VIP,
                CreatedAt = DateTime.UtcNow.AddYears(-2)
            };

            var newCustomer = new Customer
            {
                Id = 3,
                Name = "New Customer",
                Email = "new@test.com",
                Segment = CustomerSegment.Regular,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.AddRange(regularCustomer, vipCustomer, newCustomer);

            // Add test orders for VIP customer
            var vipOrders = new List<Order>
            {
                new Order
                {
                    Id = 1,
                    OrderNumber = "ORD001",
                    CustomerId = 2,
                    OrderDate = DateTime.UtcNow.AddMonths(-3),
                    Status = OrderStatus.Delivered,
                    SubTotal = 500,
                    TotalDiscount = 50,
                    ShippingCost = 10
                },
                new Order
                {
                    Id = 2,
                    OrderNumber = "ORD002",
                    CustomerId = 2,
                    OrderDate = DateTime.UtcNow.AddMonths(-1),
                    Status = OrderStatus.Delivered,
                    SubTotal = 300,
                    TotalDiscount = 30,
                    ShippingCost = 10
                }
            };

            _context.Orders.AddRange(vipOrders);

            // Add test promotions
            var promotions = new List<PromotionRule>
            {
                // Active percentage discount for VIP customers
                new PromotionRule
                {
                    Id = 1,
                    Name = "VIP 20% Discount",
                    Description = "20% off for VIP customers",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.CustomerSegment,
                    CriteriaValue = "VIP",
                    DiscountValue = 20,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    Priority = 1,
                    IsCombinable = false
                },
                // Active fixed amount discount for orders over $100
                new PromotionRule
                {
                    Id = 2,
                    Name = "$10 Off Orders Over $100",
                    Description = "Get $10 off when you spend over $100",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 10,
                    MinimumOrderAmount = 100,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    Priority = 2,
                    IsCombinable = true
                },
                // First time customer discount
                new PromotionRule
                {
                    Id = 3,
                    Name = "First Time Customer 15% Off",
                    Description = "15% discount for first-time customers",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.FirstTimeCustomer,
                    DiscountValue = 15,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    Priority = 1,
                    IsCombinable = true
                },
                // Expired promotion
                new PromotionRule
                {
                    Id = 4,
                    Name = "Expired Promotion",
                    Description = "This promotion has expired",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 50,
                    StartDate = DateTime.UtcNow.AddDays(-60),
                    EndDate = DateTime.UtcNow.AddDays(-10),
                    IsActive = true,
                    Priority = 1,
                    IsCombinable = true
                },
                // Inactive promotion
                new PromotionRule
                {
                    Id = 5,
                    Name = "Inactive Promotion",
                    Description = "This promotion is inactive",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 30,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = false,
                    Priority = 1,
                    IsCombinable = true
                },
                // Promotion with max uses per customer
                new PromotionRule
                {
                    Id = 6,
                    Name = "Limited Use Promotion",
                    Description = "Can only be used once per customer",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 25,
                    MinimumOrderAmount = 50,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    Priority = 1,
                    IsCombinable = true,
                    MaxUsesPerCustomer = 1
                },
                // Promotion for customers with minimum order count
                new PromotionRule
                {
                    Id = 7,
                    Name = "Loyal Customer Discount",
                    Description = "10% off for customers with at least 2 orders",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.OrderCount,
                    DiscountValue = 10,
                    MinimumOrderCount = 2,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    Priority = 3,
                    IsCombinable = true
                },
                // Promotion for customers with minimum total spent
                new PromotionRule
                {
                    Id = 8,
                    Name = "Big Spender Discount",
                    Description = "$50 off for customers who have spent over $500",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.TotalSpent,
                    DiscountValue = 50,
                    MinimumTotalSpent = 500,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    Priority = 1,
                    IsCombinable = false
                }
            };

            _context.PromotionRules.AddRange(promotions);

            // Add an applied discount for the limited use promotion
            _context.AppliedDiscounts.Add(new AppliedDiscount
            {
                Id = 1,
                OrderId = 1,
                PromotionRuleId = 6,
                DiscountAmount = 25,
                AppliedAt = DateTime.UtcNow.AddMonths(-1)
            });

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_VIPCustomer_ReturnsVIPPromotion()
        {
            // Arrange
            var customerId = 2; // VIP customer
            var orderSubTotal = 200m;

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customerId, orderSubTotal);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(p => p.Id == 1); // VIP discount
            result.Should().Contain(p => p.Id == 2); // Order amount discount
            result.Should().NotContain(p => p.Id == 3); // First time customer discount
            result.Should().NotContain(p => p.Id == 4); // Expired promotion
            result.Should().NotContain(p => p.Id == 5); // Inactive promotion
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_NewCustomer_ReturnsFirstTimePromotion()
        {
            // Arrange
            var customerId = 3; // New customer
            var orderSubTotal = 150m;

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customerId, orderSubTotal);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(p => p.Id == 3); // First time customer discount
            result.Should().Contain(p => p.Id == 2); // Order amount discount
            result.Should().NotContain(p => p.Id == 1); // VIP discount
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_OrderBelowMinimum_ExcludesMinimumAmountPromotions()
        {
            // Arrange
            var customerId = 3; // New customer
            var orderSubTotal = 50m;

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customerId, orderSubTotal);

            // Assert
            result.Should().NotContain(p => p.Id == 2); // Order amount discount (requires $100)
            result.Should().Contain(p => p.Id == 3); // First time customer discount (no minimum)
        }

        [Fact]
        public async Task CalculateDiscountsAsync_NonCombinablePromotion_UsesBestDiscount()
        {
            // Arrange
            var customerId = 2; // VIP customer
            var orderSubTotal = 300m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customerId, orderSubTotal, orderItems);

            // Assert
            // VIP gets 20% off $300 = $60 (non-combinable)
            // Big spender gets $50 off (non-combinable)
            // Should choose VIP discount as it's better
            totalDiscount.Should().Be(60m);
            appliedDiscounts.Should().HaveCount(1);
            appliedDiscounts[0].PromotionRuleId.Should().Be(1); // VIP discount
        }

        [Fact]
        public async Task CalculateDiscountsAsync_CombinablePromotions_AppliesMultiple()
        {
            // Arrange
            var customerId = 3; // New customer
            var orderSubTotal = 150m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customerId, orderSubTotal, orderItems);

            // Assert
            // First time customer: 15% off $150 = $22.50
            // Order over $100: $10 off
            // Total: $32.50
            totalDiscount.Should().Be(32.50m);
            appliedDiscounts.Should().HaveCount(2);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_PercentageDiscount_CalculatesCorrectly()
        {
            // Arrange
            var customerId = 2; // VIP customer
            var orderSubTotal = 250m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customerId, orderSubTotal, orderItems);

            // Assert
            // 20% of $250 = $50
            totalDiscount.Should().Be(50m);
            appliedDiscounts.Should().HaveCount(1);
            appliedDiscounts[0].DiscountAmount.Should().Be(50m);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_FixedAmountDiscount_DoesNotExceedSubtotal()
        {
            // Arrange
            var customerId = 3; // New customer
            var orderSubTotal = 8m; // Less than the fixed discount amount
            var orderItems = new List<OrderItem>();

            // Create a test promotion with fixed $10 discount
            var testPromotion = new PromotionRule
            {
                Id = 99,
                Name = "Test Fixed Discount",
                Type = PromotionType.FixedAmountDiscount,
                Criteria = PromotionCriteria.FirstTimeCustomer,
                DiscountValue = 10,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                IsActive = true,
                IsCombinable = true
            };
            _context.PromotionRules.Add(testPromotion);
            await _context.SaveChangesAsync();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customerId, orderSubTotal, orderItems);

            // Assert
            // Fixed discount should be capped at order subtotal
            appliedDiscounts.Should().Contain(d => d.DiscountAmount == 8m);
        }

        [Fact]
        public async Task IsPromotionValidForCustomerAsync_ValidPromotion_ReturnsTrue()
        {
            // Arrange
            var customerId = 2; // VIP customer
            var promotionRuleId = 1; // VIP promotion

            // Act
            var result = await _discountService.IsPromotionValidForCustomerAsync(customerId, promotionRuleId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsPromotionValidForCustomerAsync_InactivePromotion_ReturnsFalse()
        {
            // Arrange
            var customerId = 2;
            var promotionRuleId = 5; // Inactive promotion

            // Act
            var result = await _discountService.IsPromotionValidForCustomerAsync(customerId, promotionRuleId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCustomerOrderCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var customerId = 2; // VIP customer with 2 orders

            // Act
            var result = await _discountService.GetCustomerOrderCountAsync(customerId);

            // Assert
            result.Should().Be(2);
        }

        [Fact]
        public async Task GetCustomerOrderCountAsync_ExcludesCancelledOrders()
        {
            // Arrange
            var customerId = 2;

            // Add a cancelled order
            _context.Orders.Add(new Order
            {
                Id = 99,
                OrderNumber = "ORD099",
                CustomerId = 2,
                OrderDate = DateTime.UtcNow.AddDays(-5),
                Status = OrderStatus.Cancelled,
                SubTotal = 100
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _discountService.GetCustomerOrderCountAsync(customerId);

            // Assert
            result.Should().Be(2); // Should not count cancelled order
        }

        [Fact]
        public async Task GetCustomerTotalSpentAsync_ReturnsCorrectTotal()
        {
            // Arrange
            var customerId = 2; // VIP customer

            // Act
            var result = await _discountService.GetCustomerTotalSpentAsync(customerId);

            // Assert
            // Order 1: $500 - $50 + $10 = $460
            // Order 2: $300 - $30 + $10 = $280
            // Total: $740
            result.Should().Be(740m);
        }

        [Fact]
        public async Task IsFirstTimeCustomerAsync_NoOrders_ReturnsTrue()
        {
            // Arrange
            var customerId = 3; // New customer

            // Act
            var result = await _discountService.IsFirstTimeCustomerAsync(customerId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsFirstTimeCustomerAsync_HasOrders_ReturnsFalse()
        {
            // Arrange
            var customerId = 2; // VIP customer with orders

            // Act
            var result = await _discountService.IsFirstTimeCustomerAsync(customerId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetPromotionUsageCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var customerId = 2;
            var promotionRuleId = 6; // Limited use promotion

            // Act
            var result = await _discountService.GetPromotionUsageCountAsync(customerId, promotionRuleId);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_MaxUsesExceeded_ExcludesPromotion()
        {
            // Arrange
            var customerId = 2; // VIP customer who already used limited promotion
            var orderSubTotal = 100m;

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customerId, orderSubTotal);

            // Assert
            result.Should().NotContain(p => p.Id == 6); // Limited use promotion
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_MinimumOrderCountMet_IncludesPromotion()
        {
            // Arrange
            var customerId = 2; // VIP customer with 2 orders
            var orderSubTotal = 100m;

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customerId, orderSubTotal);

            // Assert
            result.Should().Contain(p => p.Id == 7); // Loyal customer discount
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_MinimumTotalSpentMet_IncludesPromotion()
        {
            // Arrange
            var customerId = 2; // VIP customer who has spent $740
            var orderSubTotal = 100m;

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customerId, orderSubTotal);

            // Assert
            result.Should().Contain(p => p.Id == 8); // Big spender discount
        }

        [Fact]
        public async Task CalculateDiscountsAsync_CreatesAppliedDiscountWithSnapshot()
        {
            // Arrange
            var customerId = 3; // New customer
            var orderSubTotal = 100m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customerId, orderSubTotal, orderItems);

            // Assert
            appliedDiscounts.Should().NotBeEmpty();
            var firstDiscount = appliedDiscounts.First();
            firstDiscount.PromotionSnapshot.Should().NotBeNullOrEmpty();

            // Verify snapshot contains promotion details
            var snapshot = JsonSerializer.Deserialize<JsonElement>(firstDiscount.PromotionSnapshot);
            snapshot.GetProperty("Name").GetString().Should().NotBeNullOrEmpty();
            snapshot.GetProperty("Type").GetString().Should().NotBeNullOrEmpty();
            snapshot.GetProperty("DiscountValue").GetDecimal().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_SortsPromotionsByPriority()
        {
            // Arrange
            var customerId = 2; // VIP customer
            var orderSubTotal = 600m;

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customerId, orderSubTotal);

            // Assert
            result.Should().BeInAscendingOrder(p => p.Priority);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_NoApplicablePromotions_ReturnsZero()
        {
            // Arrange
            var customerId = 1; // Regular customer with no applicable promotions
            var orderSubTotal = 50m; // Below minimum for most promotions
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customerId, orderSubTotal, orderItems);

            // Assert
            totalDiscount.Should().Be(0m);
            appliedDiscounts.Should().BeEmpty();
        }

        [Fact]
        public async Task CalculateDiscountsAsync_CustomerNotFound_ReturnsZero()
        {
            // Arrange
            var customerId = 999; // Non-existent customer
            var orderSubTotal = 100m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customerId, orderSubTotal, orderItems);

            // Assert
            totalDiscount.Should().Be(0m);
            appliedDiscounts.Should().BeEmpty();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}