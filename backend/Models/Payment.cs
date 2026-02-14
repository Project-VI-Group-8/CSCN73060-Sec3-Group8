using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Payment
{
	[Required]
	public Guid Id { get; set; }

	[Required]
	public Guid OrderId { get; set; }
	public Order order { get; set; } = null!;

	[Required, MaxLength(20)]
	public string Status { get; set; } = "PENDING"; // PENDING, ACCEPTED, DECLINED

	public DateTimeOffset? PaidAt { get; set; }
}

