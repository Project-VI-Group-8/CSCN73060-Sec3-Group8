using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class Product
{
	[Key]
	public int Id { get; set; }

	[Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "numeric(10,2)")]
	[Range(0, 9999999)]
	public decimal Price { get; set; }

	[Range(0, int.MaxValue)]
	public int StockQty { get; set; }
	public byte[]? ImageData { get; set; }
	public string? ImageMimeType { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}