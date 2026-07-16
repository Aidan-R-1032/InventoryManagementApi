using Microsoft.EntityFrameworkCore;
using InventoryManagementApi.Models;

namespace InventoryManagementApi.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = null;
        public DbSet<Order> Orders { get; set; } = null;
        public DbSet<OrderItem> OrderItems { get; set; } = null;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Sku)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(p => p.Sku)
                    .IsUnique();

                entity.Property(p => p.Price)
                    .HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.CustomerName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(o => o.Status)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);

                entity.Property(oi => oi.UnitPriceAtOrderTime)
                    .HasColumnType("decimal(18,2)");

                // OrderItem (many) => Order (one)
                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);          // deleting an order deletes its items

                // OrderItem (many) => Product (one)
                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);         // products cannot be deleted if its already ordered (for historical orders)
            });
        }
    }
}