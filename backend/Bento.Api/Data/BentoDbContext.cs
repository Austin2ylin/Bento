using Bento.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bento.Api.Data;

public class BentoDbContext : DbContext
{
    public BentoDbContext(DbContextOptions<BentoDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x => x.Email).IsUnique();

            // User(1) -> Order(N)
            entity.HasMany(x => x.Orders)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items");
            entity.Property(x => x.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(20);
            entity.HasIndex(x => x.OrderedAt);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");

            // 中介表主鍵：OrderId + MenuItemId
            entity.HasKey(x => new { x.OrderId, x.MenuItemId });
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);

            // Order(1) -> OrderItem(N)
            entity.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // MenuItem(1) -> OrderItem(N)
            entity.HasOne(x => x.MenuItem)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuItem>().HasData(
            new MenuItem { Id = 1, Name = "排骨便當", Price = 110, IsAvailable = true, UpdatedAt = DateTime.UtcNow },
            new MenuItem { Id = 2, Name = "雞腿便當", Price = 120, IsAvailable = true, UpdatedAt = DateTime.UtcNow },
            new MenuItem { Id = 3, Name = "鯖魚便當", Price = 130, IsAvailable = true, UpdatedAt = DateTime.UtcNow },
            new MenuItem { Id = 4, Name = "素食便當", Price = 100, IsAvailable = true, UpdatedAt = DateTime.UtcNow }
        );
    }
}
