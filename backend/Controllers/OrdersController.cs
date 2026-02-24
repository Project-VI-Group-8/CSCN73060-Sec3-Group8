using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

/// <summary>
/// Manages orders including checkout initiation and payment confirmation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
	private readonly AppDbContext _db;

	public OrdersController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Retrieves all orders with their items and payment info.
	/// </summary>
	/// <returns>A list of all orders.</returns>
	/// <response code="200">Returns the list of orders.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IEnumerable<Order>>> GetAll()
	{
		var orders = await _db.Orders
			.Include(o => o.Items)
				.ThenInclude(i => i.Product)
			.Include(o => o.Payment)
			.OrderByDescending(o => o.CreatedAt)
			.ToListAsync();

		return Ok(orders);
	}

	/// <summary>
	/// Retrieves a specific order by its ID.
	/// </summary>
	/// <param name="id">The unique identifier of the order.</param>
	/// <returns>The requested order.</returns>
	/// <response code="200">Returns the requested order.</response>
	/// <response code="404">Order not found.</response>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Order>> GetById(Guid id)
	{
		var order = await _db.Orders
			.Include(o => o.Items)
				.ThenInclude(i => i.Product)
			.Include(o => o.Payment)
			.FirstOrDefaultAsync(o => o.Id == id);

		if (order is null) return NotFound();
		return Ok(order);
	}

	/// <summary>
	/// Initiates a new checkout by creating an order with items.
	/// Validates product existence and stock availability.
	/// </summary>
	/// <param name="request">The order creation request containing userId and items.</param>
	/// <returns>The newly created order.</returns>
	/// <response code="201">Order created successfully.</response>
	/// <response code="400">Invalid request data.</response>
	/// <response code="409">Insufficient stock for one or more products.</response>
	[HttpPost]
	[ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
	{
		if (request.Items == null || request.Items.Count == 0)
			return BadRequest(new { error = "Order must contain at least one item." });

		// Validate user exists
		var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId);
		if (!userExists)
			return BadRequest(new { error = "User not found." });

		// Validate products and stock
		foreach (var item in request.Items)
		{
			var product = await _db.Products.FindAsync(item.ProductId);
			if (product is null)
				return BadRequest(new { error = $"Product {item.ProductId} not found." });
			if (product.StockQty < item.Quantity)
				return Conflict(new { error = $"Insufficient stock for product '{product.Name}'. Available: {product.StockQty}, requested: {item.Quantity}." });
		}

		var order = new Order
		{
			Id = Guid.NewGuid(),
			UserId = request.UserId,
			Status = "PENDING",
			CreatedAt = DateTimeOffset.UtcNow
		};

		// Create draft order items with unit prices
		foreach (var item in request.Items)
		{
			var product = await _db.Products.FindAsync(item.ProductId);
			order.Items.Add(new OrderItem
			{
				Id = Guid.NewGuid(),
				OrderId = order.Id,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				UnitPrice = product!.Price,
				Status = "DRAFT"
			});
		}

		// Create pending payment
		order.Payment = new Payment
		{
			Id = Guid.NewGuid(),
			OrderId = order.Id,
			Status = "PENDING"
		};

		_db.Orders.Add(order);
		await _db.SaveChangesAsync();

		return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
	}

	/// <summary>
	/// Confirms payment for an order and finalizes the checkout.
	/// Decrements stock atomically and confirms order items.
	/// </summary>
	/// <param name="id">The order ID.</param>
	/// <param name="request">The payment confirmation request containing a payment token.</param>
	/// <returns>The updated order.</returns>
	/// <response code="200">Payment confirmed, order finalized.</response>
	/// <response code="402">Payment verification failed.</response>
	/// <response code="404">Order not found.</response>
	/// <response code="409">Order is not in a confirmable state or stock insufficient.</response>
	[HttpPost("{id}/confirm-payment")]
	[ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status402PaymentRequired)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<Order>> ConfirmPayment(Guid id, [FromBody] ConfirmPaymentRequest request)
	{
		await using var transaction = await _db.Database.BeginTransactionAsync();

		try
		{
			var order = await _db.Orders
				.Include(o => o.Items)
				.Include(o => o.Payment)
				.FirstOrDefaultAsync(o => o.Id == id);

			if (order is null) return NotFound();

			if (order.Status != "PENDING")
				return Conflict(new { error = $"Order is in '{order.Status}' state and cannot be confirmed." });

			// Simulate payment verification (in production, call payment gateway)
			if (string.IsNullOrWhiteSpace(request.PaymentToken))
			{
				// Payment failed — update payment status
				if (order.Payment != null)
				{
					order.Payment.Status = "DECLINED";
				}
				order.Status = "VOID";
				await _db.SaveChangesAsync();
				await transaction.CommitAsync();
				return StatusCode(StatusCodes.Status402PaymentRequired,
					new { error = "Payment verification failed." });
			}

			// Atomically decrement stock at the database level
			foreach (var item in order.Items)
			{
				int rowsAffected = await _db.Products
					.Where(p => p.Id == item.ProductId && p.StockQty >= item.Quantity)
					.ExecuteUpdateAsync(s => s
						.SetProperty(p => p.StockQty, p => p.StockQty - item.Quantity)
						.SetProperty(p => p.UpdatedAt, _ => DateTimeOffset.UtcNow));

				if (rowsAffected == 0)
				{
					await transaction.RollbackAsync();
					return Conflict(new { error = $"Insufficient stock for product {item.ProductId}." });
				}

				item.Status = "CONFIRMED";
			}

			// Update payment and order status
			if (order.Payment != null)
			{
				order.Payment.Status = "ACCEPTED";
				order.Payment.PaidAt = DateTimeOffset.UtcNow;
			}
			order.Status = "PAID";

			await _db.SaveChangesAsync();
			await transaction.CommitAsync();

			return Ok(order);
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

	/// <summary>
	/// Updates the status of an existing order.
	/// </summary>
	/// <param name="id">The unique identifier of the order.</param>
	/// <param name="request">The update request containing the new status.</param>
	/// <returns>The updated order.</returns>
	/// <response code="200">Order updated successfully.</response>
	/// <response code="404">Order not found.</response>
	[HttpPut("{id}")]
	[ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Order>> Update(Guid id, [FromBody] UpdateOrderRequest request)
	{
		var order = await _db.Orders
			.Include(o => o.Items)
			.Include(o => o.Payment)
			.FirstOrDefaultAsync(o => o.Id == id);

		if (order is null) return NotFound();

		if (!string.IsNullOrWhiteSpace(request.Status))
			order.Status = request.Status;

		await _db.SaveChangesAsync();
		return Ok(order);
	}

	/// <summary>
	/// Deletes an order and its associated items and payment.
	/// </summary>
	/// <param name="id">The unique identifier of the order.</param>
	/// <returns>No content on success.</returns>
	/// <response code="204">Order deleted successfully.</response>
	/// <response code="404">Order not found.</response>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(Guid id)
	{
		var order = await _db.Orders.FindAsync(id);
		if (order is null) return NotFound();

		_db.Orders.Remove(order);
		await _db.SaveChangesAsync();
		return NoContent();
	}
}

// ─── Request DTOs ───────────────────────────────────────────

/// <summary>DTO for creating a new order.</summary>
public class CreateOrderRequest
{
	/// <summary>The ID of the user placing the order.</summary>
	public Guid UserId { get; set; }

	/// <summary>List of items to include in the order.</summary>
	public List<CreateOrderItemDto> Items { get; set; } = new();
}

/// <summary>DTO for an individual order item in a creation request.</summary>
public class CreateOrderItemDto
{
	/// <summary>The product ID to order.</summary>
	public int ProductId { get; set; }

	/// <summary>The quantity to order.</summary>
	public int Quantity { get; set; }
}

/// <summary>DTO for confirming payment on an order.</summary>
public class ConfirmPaymentRequest
{
	/// <summary>The payment token from the payment gateway.</summary>
	public string PaymentToken { get; set; } = string.Empty;
}

/// <summary>DTO for updating an order.</summary>
public class UpdateOrderRequest
{
	/// <summary>The new status for the order (PENDING, PAID, VOID).</summary>
	public string? Status { get; set; }
}
