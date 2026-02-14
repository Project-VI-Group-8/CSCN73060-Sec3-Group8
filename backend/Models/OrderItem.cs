using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class OrderItem
{
	[Key]
	public Guid Id { get; set; }

	[Required]
	public Guid OrderId { get; set; }
	public Order Order { get; set; } = null!;

	[Required]
	public int ProductId {  get; set; }
	public Product Product { get; set; } = null!;

	[Range(1, int.MaxValue)]
	public int Quantity { get; set; }
}

