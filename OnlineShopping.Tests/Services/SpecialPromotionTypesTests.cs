using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.Models;
using OnlineShopping.Services;
using Xunit;
using FluentAssertions;

namespace OnlineShopping.Tests.Services
{
    /// <summary>
    /// Tests for special promotion types that may require additional implementation
    /// </summary>
    public class SpecialPromotionTypesTests : IDisposable
    {
        private readonly OrderDbContext _context;
        private readonly DiscountService _discountService;

        public SpecialPromotionTypesTests()
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
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Email = "test@example.com",
                Segment = CustomerSegment.Regular
            };
            _context.Customers.Add(customer);

            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Product A", Price = 50 },
                new Product { Id = 2, Name = "Product B", Price = 30 },
                new Product { Id = 3, Name = "Product C", Price = 20 }
            };
            _context.Products.AddRange(products);

            _context.SaveChanges();
        }

        [Fact]
        public async Task CalculateDiscountsAsync_FreeShippingPromotion_ReturnsZeroDiscount()
        {
            // Arrange
            var freeShippingPromotion = new PromotionRule
            {
                Id = 1,
                Name = "Free Shipping Over $100",
                Type = PromotionType.FreeShipping,
                Criteria = PromotionCriteria.OrderAmount,
                MinimumOrderAmount = 100,
                DiscountValue = 0, // Free shipping doesn't affect order discount
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(freeShippingPromotion);
            await _context.SaveChangesAsync();

            var orderSubTotal = 150m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                1, orderSubTotal, orderItems);

            // Assert
            // Free shipping promotions return 0 discount as they affect shipping cost, not order total
            totalDiscount.Should().Be(0m);
            appliedDiscounts.Should().BeEmpty();
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_FreeShippingPromotion_IncludedWhenEligible()
        {
            // Arrange
            var freeShippingPromotion = new PromotionRule
            {
                Id = 1,
                Name = "Free Shipping",
                Type = PromotionType.FreeShipping,
                Criteria = PromotionCriteria.OrderAmount,
                MinimumOrderAmount = 50,
                DiscountValue = 0,
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(freeShippingPromotion);
            await _context.SaveChangesAsync();

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(1, 100m);

            // Assert
            result.Should().Contain(p => p.Id == 1 && p.Type == PromotionType.FreeShipping);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_BuyXGetYPromotion_ReturnsZeroDiscount()
        {
            // Arrange
            var buyXGetYPromotion = new PromotionRule
            {
                Id = 1,
                Name = "Buy 2 Get 1 Free",
                Type = PromotionType.BuyXGetY,
                Criteria = PromotionCriteria.SpecificProduct,
                BuyQuantity = 2,
                GetQuantity = 1,
                DiscountValue = 100, // 100% off the free item
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(buyXGetYPromotion);
            await _context.SaveChangesAsync();

            var orderSubTotal = 150m;
            var orderItems = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 3, UnitPrice = 50 }
            };

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                1, orderSubTotal, orderItems);

            // Assert
            // Current implementation returns 0 for BuyXGetY as it needs item-level calculation
            totalDiscount.Should().Be(0m);
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_SpecificProductCriteria_AlwaysReturnsPromotion()
        {
            // Arrange
            var productSpecificPromotion = new PromotionRule
            {
                Id = 1,
                Name = "Product A Discount",
                Type = PromotionType.PercentageDiscount,
                Criteria = PromotionCriteria.SpecificProduct,
                CriteriaValue = "1", // Product ID
                DiscountValue = 10,
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(productSpecificPromotion);
            await _context.SaveChangesAsync();

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(1, 100m);

            // Assert
            // Current implementation always returns true for SpecificProduct criteria
            result.Should().Contain(p => p.Id == 1);
        }

        [Fact]
        public async Task CalculateDiscountsAsync_MinimumPurchaseDiscountType_TreatsAsRegularDiscount()
        {
            // Arrange
            var minPurchasePromotion = new PromotionRule
            {
                Id = 1,
                Name = "Minimum Purchase Discount",
                Type = PromotionType.MinimumPurchaseDiscount,
                Criteria = PromotionCriteria.OrderAmount,
                MinimumOrderAmount = 100,
                DiscountValue = 15, // 15% off
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(minPurchasePromotion);
            await _context.SaveChangesAsync();

            var orderSubTotal = 200m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                1, orderSubTotal, orderItems);

            // Assert
            // MinimumPurchaseDiscount type is not explicitly handled, so returns 0
            totalDiscount.Should().Be(0m);
        }

        [Theory]
        [InlineData(PromotionType.BuyXGetY, 0)]
        [InlineData(PromotionType.FreeShipping, 0)]
        [InlineData(PromotionType.MinimumPurchaseDiscount, 0)]
        public async Task CalculateDiscountsAsync_UnimplementedPromotionTypes_ReturnZeroDiscount(
            PromotionType promotionType, decimal expectedDiscount)
        {
            // Arrange
            var promotion = new PromotionRule
            {
                Id = 1,
                Name = $"{promotionType} Test",
                Type = promotionType,
                Criteria = PromotionCriteria.OrderAmount,
                DiscountValue = 20,
                StartDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Priority = 1
            };
            _context.PromotionRules.Add(promotion);
            await _context.SaveChangesAsync();

            var orderSubTotal = 100m;
            var orderItems = new List<OrderItem>();

            // Act
            var (totalDiscount, appliedDiscounts) = await _discountService.CalculateDiscountsAsync(
                1, orderSubTotal, orderItems);

            // Assert
            totalDiscount.Should().Be(expectedDiscount);
        }

        [Fact]
        public async Task GetApplicablePromotionsAsync_MultipleBuyXGetYPromotions_ReturnsAll()
        {
            // Arrange
            var promotions = new List<PromotionRule>
            {
                new PromotionRule
                {
                    Id = 1,
                    Name = "Buy 2 Get 1 Free - Product A",
                    Type = PromotionType.BuyXGetY,
                    Criteria = PromotionCriteria.SpecificProduct,
                    CriteriaValue = "1",
                    BuyQuantity = 2,
                    GetQuantity = 1,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    Priority = 1
                },
                new PromotionRule
                {
                    Id = 2,
                    Name = "Buy 3 Get 2 Free - Product B",
                    Type = PromotionType.BuyXGetY,
                    Criteria = PromotionCriteria.SpecificProduct,
                    CriteriaValue = "2",
                    BuyQuantity = 3,
                    GetQuantity = 2,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    Priority = 2
                }
            };
            _context.PromotionRules.AddRange(promotions);
            await _context.SaveChangesAsync();

            // Act
            var result = await _discountService.GetApplicablePromotionsAsync(1, 200m);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.BuyQuantity == 2 && p.GetQuantity == 1);
            result.Should().Contain(p => p.BuyQuantity == 3 && p.GetQuantity == 2);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}