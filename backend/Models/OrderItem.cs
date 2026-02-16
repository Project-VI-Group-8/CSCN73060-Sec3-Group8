using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

	[Column(TypeName = "numeric(10,2)")]
	[Range(0, 9999999)]
	public decimal UnitPrice { get; set; }

	[MaxLength(20)]
	public string Status { get; set; } = "DRAFT"; // DRAFT, CONFIRMED
}

