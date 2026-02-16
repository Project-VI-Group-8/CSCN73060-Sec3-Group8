using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

/// <summary>
/// Manages payment records associated with orders.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
	private readonly AppDbContext _db;

	public PaymentsController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Retrieves all payment records.
	/// </summary>
	/// <returns>A list of all payments.</returns>
	/// <response code="200">Returns the list of payments.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<Payment>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IEnumerable<Payment>>> GetAll()
	{
		var payments = await _db.Payments.ToListAsync();
		return Ok(payments);
	}

	/// <summary>
	/// Retrieves a specific payment by its ID.
	/// </summary>
	/// <param name="id">The unique identifier of the payment.</param>
	/// <returns>The requested payment.</returns>
	/// <response code="200">Returns the requested payment.</response>
	/// <response code="404">Payment not found.</response>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(Payment), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Payment>> GetById(Guid id)
	{
		var payment = await _db.Payments.FindAsync(id);
		if (payment is null) return NotFound();
		return Ok(payment);
	}

	/// <summary>
	/// Creates a new payment for an order.
	/// </summary>
	/// <param name="request">The payment creation request.</param>
	/// <returns>The newly created payment.</returns>
	/// <response code="201">Payment created successfully.</response>
	/// <response code="400">Invalid request or order not found.</response>
	/// <response code="409">A payment already exists for this order.</response>
	[HttpPost]
	[ProducesResponseType(typeof(Payment), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<Payment>> Create([FromBody] CreatePaymentRequest request)
	{
		var orderExists = await _db.Orders.AnyAsync(o => o.Id == request.OrderId);
		if (!orderExists)
			return BadRequest(new { error = "Order not found." });

		var existingPayment = await _db.Payments.AnyAsync(p => p.OrderId == request.OrderId);
		if (existingPayment)
			return Conflict(new { error = "A payment already exists for this order." });

		var payment = new Payment
		{
			Id = Guid.NewGuid(),
			OrderId = request.OrderId,
			Status = "PENDING"
		};

		_db.Payments.Add(payment);
		await _db.SaveChangesAsync();

		return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
	}

	/// <summary>
	/// Updates the status of an existing payment.
	/// </summary>
	/// <param name="id">The unique identifier of the payment.</param>
	/// <param name="request">The update request containing the new status.</param>
	/// <returns>The updated payment.</returns>
	/// <response code="200">Payment updated successfully.</response>
	/// <response code="404">Payment not found.</response>
	[HttpPut("{id}")]
	[ProducesResponseType(typeof(Payment), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Payment>> Update(Guid id, [FromBody] UpdatePaymentRequest request)
	{
		var payment = await _db.Payments.FindAsync(id);
		if (payment is null) return NotFound();

		if (!string.IsNullOrWhiteSpace(request.Status))
		{
			payment.Status = request.Status;

			// Auto-set PaidAt when payment is accepted
			if (request.Status == "ACCEPTED" && payment.PaidAt is null)
				payment.PaidAt = DateTimeOffset.UtcNow;
		}

		await _db.SaveChangesAsync();
		return Ok(payment);
	}

	/// <summary>
	/// Deletes a payment record.
	/// </summary>
	/// <param name="id">The unique identifier of the payment.</param>
	/// <returns>No content on success.</returns>
	/// <response code="204">Payment deleted successfully.</response>
	/// <response code="404">Payment not found.</response>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(Guid id)
	{
		var payment = await _db.Payments.FindAsync(id);
		if (payment is null) return NotFound();

		_db.Payments.Remove(payment);
		await _db.SaveChangesAsync();
		return NoContent();
	}
}

// ─── Request DTOs ───────────────────────────────────────────

/// <summary>DTO for creating a payment.</summary>
public class CreatePaymentRequest
{
	/// <summary>The ID of the order this payment is for.</summary>
	public Guid OrderId { get; set; }
}

/// <summary>DTO for updating a payment status.</summary>
public class UpdatePaymentRequest
{
	/// <summary>The new payment status (PENDING, ACCEPTED, DECLINED).</summary>
	public string? Status { get; set; }
}
