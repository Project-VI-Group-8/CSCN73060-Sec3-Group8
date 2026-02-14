using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions options) : base(options) {}

	public DbSet<Product> Products => Set<Product>();
	public DbSet<Order> Orders => Set<Order>();
	public DbSet<OrderItem> OrderItems => Set<OrderItem>();
	public DbSet<Payment> Payments => Set<Payment>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Products
		modelBuilder.Entity<Product>()
			.HasIndex(p => p.Name);

		// Order -> OrderItems (1 to many)
		modelBuilder.Entity<OrderItem>()
			.HasOne(oi => oi.Order)
			.WithMany(o => o.Items)
			.HasForeignKey(oi => oi.OrderId)
			.OnDelete(DeleteBehavior.Cascade);

		// OrderItem -> Product (many to 1)
		modelBuilder.Entity<OrderItem>()
			.HasOne(oi => oi.Product)
			.WithMany()
			.HasForeignKey(oi => oi.ProductId)
			.OnDelete(DeleteBehavior.Cascade);

		// Order -> Payment (1 to 1)
		modelBuilder.Entity<Payment>()
			.HasOne(p => p.order)
			.WithOne(o => o.Payment)
			.HasForeignKey<Payment>(p => p.OrderId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Payment>()
			.HasIndex(p => p.OrderId)
			.IsUnique();
	}
}