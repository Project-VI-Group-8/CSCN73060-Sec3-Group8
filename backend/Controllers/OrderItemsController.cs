using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

/// <summary>
/// Manages individual order line items.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrderItemsController : ControllerBase
{
	private readonly AppDbContext _db;

	public OrderItemsController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Retrieves all order items.
	/// </summary>
	/// <returns>A list of all order items.</returns>
	/// <response code="200">Returns the list of order items.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<OrderItem>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IEnumerable<OrderItem>>> GetAll()
	{
		var items = await _db.OrderItems
			.Include(oi => oi.Product)
			.ToListAsync();

		return Ok(items);
	}

	/// <summary>
	/// Retrieves a specific order item by its ID.
	/// </summary>
	/// <param name="id">The unique identifier of the order item.</param>
	/// <returns>The requested order item.</returns>
	/// <response code="200">Returns the requested order item.</response>
	/// <response code="404">Order item not found.</response>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(OrderItem), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<OrderItem>> GetById(Guid id)
	{
		var item = await _db.OrderItems
			.Include(oi => oi.Product)
			.FirstOrDefaultAsync(oi => oi.Id == id);

		if (item is null) return NotFound();
		return Ok(item);
	}

	/// <summary>
	/// Adds a new item to an existing order.
	/// </summary>
	/// <param name="request">The order item creation request.</param>
	/// <returns>The newly created order item.</returns>
	/// <response code="201">Order item created successfully.</response>
	/// <response code="400">Invalid request data or referenced order/product not found.</response>
	[HttpPost]
	[ProducesResponseType(typeof(OrderItem), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<OrderItem>> Create([FromBody] CreateOrderItemRequest request)
	{
		var orderExists = await _db.Orders.AnyAsync(o => o.Id == request.OrderId);
		if (!orderExists)
			return BadRequest(new { error = "Order not found." });

		var product = await _db.Products.FindAsync(request.ProductId);
		if (product is null)
			return BadRequest(new { error = "Product not found." });

		var item = new OrderItem
		{
			Id = Guid.NewGuid(),
			OrderId = request.OrderId,
			ProductId = request.ProductId,
			Quantity = request.Quantity,
			UnitPrice = product.Price,
			Status = "DRAFT"
		};

		_db.OrderItems.Add(item);
		await _db.SaveChangesAsync();

		return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
	}

	/// <summary>
	/// Updates an existing order item's quantity.
	/// </summary>
	/// <param name="id">The unique identifier of the order item.</param>
	/// <param name="request">The update request.</param>
	/// <returns>The updated order item.</returns>
	/// <response code="200">Order item updated successfully.</response>
	/// <response code="404">Order item not found.</response>
	[HttpPut("{id}")]
	[ProducesResponseType(typeof(OrderItem), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<OrderItem>> Update(Guid id, [FromBody] UpdateOrderItemRequest request)
	{
		var item = await _db.OrderItems.FindAsync(id);
		if (item is null) return NotFound();

		if (request.Quantity.HasValue)
			item.Quantity = request.Quantity.Value;

		if (!string.IsNullOrWhiteSpace(request.Status))
			item.Status = request.Status;

		await _db.SaveChangesAsync();
		return Ok(item);
	}

	/// <summary>
	/// Removes an order item.
	/// </summary>
	/// <param name="id">The unique identifier of the order item.</param>
	/// <returns>No content on success.</returns>
	/// <response code="204">Order item deleted successfully.</response>
	/// <response code="404">Order item not found.</response>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(Guid id)
	{
		var item = await _db.OrderItems.FindAsync(id);
		if (item is null) return NotFound();

		_db.OrderItems.Remove(item);
		await _db.SaveChangesAsync();
		return NoContent();
	}
}

// ─── Request DTOs ───────────────────────────────────────────

/// <summary>DTO for adding an item to an order.</summary>
public class CreateOrderItemRequest
{
	/// <summary>The ID of the order to add the item to.</summary>
	public Guid OrderId { get; set; }

	/// <summary>The product ID.</summary>
	public int ProductId { get; set; }

	/// <summary>The quantity to order.</summary>
	public int Quantity { get; set; }
}

/// <summary>DTO for updating an order item.</summary>
public class UpdateOrderItemRequest
{
	/// <summary>The new quantity (optional).</summary>
	public int? Quantity { get; set; }

	/// <summary>The new status (optional, e.g. DRAFT, CONFIRMED).</summary>
	public string? Status { get; set; }
}
