using Microsoft.EntityFrameworkCore;
using OnlineShopping.Models;

namespace OnlineShopping.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
        public DbSet<PromotionRule> PromotionRules { get; set; }
        public DbSet<AppliedDiscount> AppliedDiscounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed data for products
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Price = 999.99m },
                new Product { Id = 2, Name = "Mouse", Price = 29.99m },
                new Product { Id = 3, Name = "Keyboard", Price = 79.99m }
            );

            // Configure relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);

            modelBuilder.Entity<AppliedDiscount>()
                .HasOne(ad => ad.Order)
                .WithMany(o => o.AppliedDiscounts)
                .HasForeignKey(ad => ad.OrderId);

            modelBuilder.Entity<AppliedDiscount>()
                .HasOne(ad => ad.PromotionRule)
                .WithMany(pr => pr.AppliedDiscounts)
                .HasForeignKey(ad => ad.PromotionRuleId);

            // Configure decimal precision
            modelBuilder.Entity<Order>()
                .Property(o => o.SubTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalDiscount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.ItemDiscount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PromotionRule>()
                .Property(pr => pr.DiscountValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<AppliedDiscount>()
                .Property(ad => ad.DiscountAmount)
                .HasPrecision(18, 2);

            // Seed initial promotion rules with fixed date
            var seedDate = new DateTime(2024, 1, 1);
            modelBuilder.Entity<PromotionRule>().HasData(
                new PromotionRule
                {
                    Id = 1,
                    Name = "VIP 20% Discount",
                    Description = "20% discount for VIP customers",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.CustomerSegment,
                    DiscountValue = 20,
                    CriteriaValue = "VIP",
                    StartDate = seedDate,
                    IsActive = true,
                    Priority = 1
                },
                new PromotionRule
                {
                    Id = 2,
                    Name = "Premium 10% Discount",
                    Description = "10% discount for Premium customers",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.CustomerSegment,
                    DiscountValue = 10,
                    CriteriaValue = "Premium",
                    StartDate = seedDate,
                    IsActive = true,
                    Priority = 2
                },
                new PromotionRule
                {
                    Id = 3,
                    Name = "First Time Customer Discount",
                    Description = "$50 off for first-time customers on orders over $200",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.FirstTimeCustomer,
                    DiscountValue = 50,
                    MinimumOrderAmount = 200,
                    StartDate = seedDate,
                    IsActive = true,
                    Priority = 3
                },
                new PromotionRule
                {
                    Id = 4,
                    Name = "Loyal Customer Reward",
                    Description = "15% off for customers with 5+ orders",
                    Type = PromotionType.PercentageDiscount,
                    Criteria = PromotionCriteria.OrderCount,
                    DiscountValue = 15,
                    MinimumOrderCount = 5,
                    StartDate = seedDate,
                    IsActive = true,
                    Priority = 2
                },
                new PromotionRule
                {
                    Id = 5,
                    Name = "Big Spender Bonus",
                    Description = "$100 off for customers who have spent over $5000",
                    Type = PromotionType.FixedAmountDiscount,
                    Criteria = PromotionCriteria.TotalSpent,
                    DiscountValue = 100,
                    MinimumTotalSpent = 5000,
                    StartDate = seedDate,
                    IsActive = true,
                    Priority = 1,
                    MaxUsesPerCustomer = 1
                }
            );
        }
    }
}