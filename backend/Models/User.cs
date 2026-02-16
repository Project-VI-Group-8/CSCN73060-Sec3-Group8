using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class User
{
	[Key]
	public Guid Id { get; set; }

	[Required, MaxLength(200)]
	public string Email { get; set; } = string.Empty;

	[Required, MaxLength(100)]
	public string Name { get; set; } = string.Empty;

	[MaxLength(500)]
	public string? Address { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	public List<Order> Orders { get; set; } = new();
}
