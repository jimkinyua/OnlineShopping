using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.Models;
using OnlineShopping.Services;
using Xunit;
using FluentAssertions;

namespace OnlineShopping.Tests.Services
{
    public class DiscountCalculationEdgeCaseTests : IDisposable
    {
        private readonly OrderDbContext _context;
        private readonly DiscountService _discountService;

        public DiscountCalculationEdgeCaseTests()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new OrderDbContext(options);
            _discountService = new DiscountService(_context);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_MultiplePercentageDiscounts_AppliesSequentially()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.VIP
            };
            _context.Customers.Add(customer);

            var promotions = new List<PromotionRule>
            {
                new PromotionRule
                {
                    Id = 1,
                    Name = "10% Off",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 10,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 1
                },
                new PromotionRule
                {
                    Id = 2,
                    Name = "5% Off",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 5,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 2
                }
            };
            _context.PromotionRules.AddRange(promotions);
            await _context.SaveChangesAsync();

            var orderSubTotal = 100m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customer.Id, orderSubTotal, orderItems);

            // Assert
            // First discount: 10% of $100 = $10
            // Second discount: 5% of $90 = $4.50
            // Total: $14.50
            totalDiscount.Should().Be(14.50m);
            appliedDiscounts.Should().HaveCount(2);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_MixedDiscountTypes_CalculatesCorrectly()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var promotions = new List<PromotionRule>
            {
                new PromotionRule
                {
                    Id = 1,
                    Name = "15% Off",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 15,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 1
                },
                new PromotionRule
                {
                    Id = 2,
                    Name = "$20 Off",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 20,
                    MinimumOrderAmount = 100,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 2
                }
            };
            _context.PromotionRules.AddRange(promotions);
            await _context.SaveChangesAsync();

            var orderSubTotal = 200m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customer.Id, orderSubTotal, orderItems);

            // Assert
            // First discount: 15% of $200 = $30
            // Second discount: $20 fixed
            // Total: $50
            totalDiscount.Should().Be(50m);
            appliedDiscounts.Should().HaveCount(2);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_DiscountExceedsSubtotal_CapsAtSubtotal()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var promotions = new List<PromotionRule>
            {
                new PromotionRule
                {
                    Id = 1,
                    Name = "$60 Off",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 60,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 1
                },
                new PromotionRule
                {
                    Id = 2,
                    Name = "$50 Off",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 50,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 2
                }
            };
            _context.PromotionRules.AddRange(promotions);
            await _context.SaveChangesAsync();

            var orderSubTotal = 80m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customer.Id, orderSubTotal, orderItems);

            // Assert
            // First discount should be $60 but capped at subtotal
            // Second discount would make total exceed subtotal, so it should be adjusted
            totalDiscount.Should().BeLessOrEqualTo(orderSubTotal);
            appliedDiscounts.Should().HaveCount(2);
            appliedDiscounts[0].DiscountAmount.Should().Be(60m);
            appliedDiscounts[1].DiscountAmount.Should().Be(20m); // Remaining amount
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_FutureStartDate_ExcludesPromotion()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var futurePromotion = new PromotionRule
            {
                Id = 1,
                Name = "Future Promotion",
                Type = PromotionType.PercentageDiscount,
                Criteria = PromotionCriteria.OrderAmount,
                DiscountValue = 20,
                StartDate = DateTime.UtcNow.AddDays(1), // Starts tomorrow
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(futurePromotion);
            await _context.SaveChangesAsync();

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customer.Id, 100m);

            // Assert
            result.Should().NotContain(p => p.Id == 1);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_ZeroOrderAmount_HandlesGracefully()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var promotion = new PromotionRule
            {
                Id = 1,
                Name = "10% Off",
                Type = PromotionType.PercentageDiscount,
                Criteria = PromotionCriteria.OrderAmount,
                DiscountValue = 10,
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(promotion);
            await _context.SaveChangesAsync();

            var orderSubTotal = 0m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customer.Id, orderSubTotal, orderItems);

            // Assert
            totalDiscount.Should().Be(0m);
            appliedDiscounts.Should().HaveCount(1);
            appliedDiscounts[0].DiscountAmount.Should().Be(0m);
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_NullEndDate_TreatsAsNoExpiry()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var promotion = new PromotionRule
            {
                Id = 1,
                Name = "No Expiry Promotion",
                Type = PromotionType.PercentageDiscount,
                Criteria = PromotionCriteria.OrderAmount,
                DiscountValue = 10,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = null, // No expiry
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(promotion);
            await _context.SaveChangesAsync();

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(customer.Id, 100m);

            // Assert
            result.Should().Contain(p => p.Id == 1);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_EmptyOrderItemsList_StillCalculatesOrderLevelDiscounts()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var promotion = new PromotionRule
            {
                Id = 1,
                Name = "Order Level Discount",
                Type = PromotionType.PercentageDiscount,
                Criteria = PromotionCriteria.OrderAmount,
                DiscountValue = 10,
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(promotion);
            await _context.SaveChangesAsync();

            var orderSubTotal = 100m;
            var orderItems = new List<OrderItem>(); // Empty list

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customer.Id, orderSubTotal, orderItems);

            // Assert
            totalDiscount.Should().Be(10m);
            appliedDiscounts.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetCustomerTotalSpentAsync_NullAmounts_HandlesGracefully()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var order = new Order
            {
                Id = 1,
                OrderNumber = "ORD001",
                CustomerId = 1,
                OrderDate = DateTime.UtcNow.AddDays(-10),
                Status = OrderStatus.Delivered,
                SubTotal = 100,
                TotalDiscount = 0,
                ShippingCost = 0
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _discountService.GetCustomerTotalSpentAsync(customer.Id);

            // Assert
            result.Should().Be(100m); // SubTotal - TotalDiscount + ShippingCost
        }

        [Fact]
        public async Task CalculateDiscountsAsync_PromotionWithSamePriority_AppliesInIdOrder()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var promotions = new List<PromotionRule>
            {
                new PromotionRule
                {
                    Id = 2,
                    Name = "Second Added",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 10,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 1 // Same priority
                },
                new PromotionRule
                {
                    Id = 1,
                    Name = "First Added",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.OrderAmount,
                    DiscountValue = 15,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    IsCombinable = true,
                    Priority = 1 // Same priority
                }
            };
            _context.PromotionRules.AddRange(promotions);
            await _context.SaveChangesAsync();

            var orderSubTotal = 100m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                customer.Id, orderSubTotal, orderItems);

            // Assert
            totalDiscount.Should().Be(25m);
            appliedDiscounts.Should().HaveCount(2);
            // With same priority, they should be applied in ID order (1, then 2)
            appliedDiscounts[0].PromotionRuleId.Should().Be(1);
            appliedDiscounts[1].PromotionRuleId.Should().Be(2);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}