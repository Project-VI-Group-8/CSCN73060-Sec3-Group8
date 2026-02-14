using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Order
{
	[Key]
	public Guid Id { get; set; }

	[Required, MaxLength(20)]
	public string Status { get; set; } = "PENDING"; // PENDING, PAID, VOID

	public DateTimeOffset CreatedAt { get; set; } =	DateTimeOffset.UtcNow;

	public List<OrderItem> Items { get; set; } = new();
	public Payment? Payment { get; set; }
}
