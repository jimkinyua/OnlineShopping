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
        public DbSet<Promotion> Promotions { get; set; }
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

            // Seed data for promotions
            modelBuilder.Entity<Promotion>().HasData(
                new Promotion
                {
                    Id = 1,
                    Name = "VIP 20% Off",
                    Description = "20% discount for VIP customers",
                    Type = PromotionType.PercentageDiscount,
                    DiscountValue = 20,
                    TargetSegment = CustomerSegment.VIP,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddMonths(12),
                    IsActive = true,
                    Priority = 10
                },
                new Promotion
                {
                    Id = 2,
                    Name = "Premium 10% Off",
                    Description = "10% discount for Premium customers",
                    Type = PromotionType.PercentageDiscount,
                    DiscountValue = 10,
                    TargetSegment = CustomerSegment.Premium,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddMonths(12),
                    IsActive = true,
                    Priority = 5
                },
                new Promotion
                {
                    Id = 3,
                    Name = "Loyalty Discount",
                    Description = "$50 off for customers with 5+ orders",
                    Type = PromotionType.FixedAmountDiscount,
                    DiscountValue = 50,
                    MinimumOrderCount = 5,
                    MinimumPurchaseAmount = 200,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddMonths(12),
                    IsActive = true,
                    Priority = 8
                },
                new Promotion
                {
                    Id = 4,
                    Name = "Big Spender Discount",
                    Description = "15% off orders over $500",
                    Type = PromotionType.PercentageDiscount,
                    DiscountValue = 15,
                    MinimumPurchaseAmount = 500,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddMonths(12),
                    IsActive = true,
                    Priority = 7
                }
            );
        }
    }
}