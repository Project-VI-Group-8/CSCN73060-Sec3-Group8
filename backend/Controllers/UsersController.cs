using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

/// <summary>
/// Manages user accounts for the VelocityRetail platform.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
	private readonly AppDbContext _db;

	public UsersController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Retrieves all registered users.
	/// </summary>
	/// <returns>A list of all users.</returns>
	/// <response code="200">Returns the list of users.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IEnumerable<User>>> GetAll()
	{
		var users = await _db.Users
			.OrderByDescending(u => u.CreatedAt)
			.ToListAsync();

		return Ok(users);
	}

	/// <summary>
	/// Retrieves a specific user by their ID.
	/// </summary>
	/// <param name="id">The unique identifier of the user.</param>
	/// <returns>The requested user.</returns>
	/// <response code="200">Returns the requested user.</response>
	/// <response code="404">User not found.</response>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<User>> GetById(Guid id)
	{
		var user = await _db.Users.FindAsync(id);
		if (user is null) return NotFound();
		return Ok(user);
	}

	/// <summary>
	/// Registers a new user account.
	/// </summary>
	/// <param name="request">The user registration request containing name, email, and address.</param>
	/// <returns>The newly created user.</returns>
	/// <response code="201">User created successfully.</response>
	/// <response code="400">Invalid request data.</response>
	/// <response code="409">A user with this email already exists.</response>
	[HttpPost]
	[ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<User>> Create([FromBody] CreateUserRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Email))
			return BadRequest(new { error = "Email is required." });

		if (string.IsNullOrWhiteSpace(request.Name))
			return BadRequest(new { error = "Name is required." });

		var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
		if (emailExists)
			return Conflict(new { error = "A user with this email already exists." });

		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = request.Email.Trim(),
			Name = request.Name.Trim(),
			Address = request.Address?.Trim(),
			CreatedAt = DateTimeOffset.UtcNow
		};

		_db.Users.Add(user);
		await _db.SaveChangesAsync();

		return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
	}

	/// <summary>
	/// Partially updates a user's details. Only provided fields are updated.
	/// </summary>
	/// <param name="id">The unique identifier of the user.</param>
	/// <param name="request">The fields to update (name, email, and/or address).</param>
	/// <returns>The updated user.</returns>
	/// <response code="200">User updated successfully.</response>
	/// <response code="400">Invalid request data.</response>
	/// <response code="404">User not found.</response>
	/// <response code="409">The new email is already in use by another user.</response>
	[HttpPatch("{id}")]
	[ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<User>> Patch(Guid id, [FromBody] PatchUserRequest request)
	{
		var user = await _db.Users.FindAsync(id);
		if (user is null) return NotFound();

		if (request.Email is not null)
		{
			var trimmedEmail = request.Email.Trim();
			if (string.IsNullOrWhiteSpace(trimmedEmail))
				return BadRequest(new { error = "Email cannot be empty." });

			var emailTaken = await _db.Users.AnyAsync(u => u.Email == trimmedEmail && u.Id != id);
			if (emailTaken)
				return Conflict(new { error = "A user with this email already exists." });

			user.Email = trimmedEmail;
		}

		if (request.Name is not null)
		{
			var trimmedName = request.Name.Trim();
			if (string.IsNullOrWhiteSpace(trimmedName))
				return BadRequest(new { error = "Name cannot be empty." });

			user.Name = trimmedName;
		}

		if (request.Address is not null)
			user.Address = request.Address.Trim();

		await _db.SaveChangesAsync();
		return Ok(user);
	}

	/// <summary>
	/// Deletes a user account. Fails if the user has existing orders.
	/// </summary>
	/// <param name="id">The unique identifier of the user.</param>
	/// <returns>No content on success.</returns>
	/// <response code="204">User deleted successfully.</response>
	/// <response code="404">User not found.</response>
	/// <response code="409">User has existing orders and cannot be deleted.</response>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Delete(Guid id)
	{
		var user = await _db.Users.FindAsync(id);
		if (user is null) return NotFound();

		var hasOrders = await _db.Orders.AnyAsync(o => o.UserId == id);
		if (hasOrders)
			return Conflict(new { error = "Cannot delete user with existing orders." });

		_db.Users.Remove(user);
		await _db.SaveChangesAsync();
		return NoContent();
	}
}

// ─── Request DTOs ───────────────────────────────────────────

/// <summary>DTO for creating a new user.</summary>
public class CreateUserRequest
{
	/// <summary>The user's email address (must be unique).</summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>The user's full name.</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>The user's address (optional).</summary>
	public string? Address { get; set; }
}

/// <summary>DTO for partially updating a user. Only non-null fields are applied.</summary>
public class PatchUserRequest
{
	/// <summary>New email (optional, must be unique if provided).</summary>
	public string? Email { get; set; }

	/// <summary>New name (optional).</summary>
	public string? Name { get; set; }

	/// <summary>New address (optional).</summary>
	public string? Address { get; set; }
}
