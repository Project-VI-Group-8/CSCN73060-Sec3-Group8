using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Order
{
	[Key]
	public Guid Id { get; set; }

	[Required]
	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	[Required, MaxLength(20)]
	public string Status { get; set; } = "PENDING"; // PENDING, PAID, VOID

	public DateTimeOffset CreatedAt { get; set; } =	DateTimeOffset.UtcNow;

	public List<OrderItem> Items { get; set; } = new();
	public Payment? Payment { get; set; }
}
