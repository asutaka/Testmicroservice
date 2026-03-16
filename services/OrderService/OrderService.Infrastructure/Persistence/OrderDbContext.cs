using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence;

public sealed class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).ValueGeneratedNever();
            builder.Property(o => o.UserId).IsRequired();
            builder.Property(o => o.Status).IsRequired();
            builder.Property(o => o.Notes).HasMaxLength(500);
            builder.Ignore(o => o.DomainEvents);

            // Money owned entity
            builder.OwnsOne(o => o.TotalAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 4);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            // One-to-many with OrderItems
            builder.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("Orders");
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).ValueGeneratedNever();
            builder.Property(i => i.ProductId).IsRequired().HasMaxLength(50);
            builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            builder.Property(i => i.Quantity).IsRequired();
            builder.Property(i => i.UnitPrice).IsRequired().HasPrecision(18, 4);
            builder.Ignore(i => i.DomainEvents);
            builder.ToTable("OrderItems");
        });
    }
}
