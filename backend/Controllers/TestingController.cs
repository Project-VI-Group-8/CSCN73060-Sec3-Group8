using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

/// <summary>
/// Development-only endpoints for generating test data and managing test state.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestingController : ControllerBase
{
	private readonly AppDbContext _db;
	private static readonly Random _rng = new();

	// ─── Sample Data Pools ──────────────────────────────────
	private static readonly string[] FirstNames = { "Alice", "Bob", "Charlie", "Diana", "Ethan", "Fiona", "George", "Hannah", "Ivan", "Julia", "Kevin", "Luna", "Marcus", "Nina", "Oscar", "Priya", "Quinn", "Rosa", "Sam", "Tara" };
	private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Harris", "Clark" };
	private static readonly string[] Streets = { "123 Main St", "456 Oak Ave", "789 Pine Rd", "321 Elm Blvd", "654 Maple Dr", "987 Cedar Ln", "147 Birch Way", "258 Walnut Ct", "369 Spruce Pl", "741 Willow Cir" };
	private static readonly string[] Cities = { "Toronto", "Vancouver", "Montreal", "Calgary", "Ottawa", "Winnipeg", "Halifax", "Victoria", "Kitchener", "Hamilton" };
	private static readonly string[] ProductAdjectives = { "Premium", "Classic", "Ultra", "Pro", "Essential", "Deluxe", "Eco", "Smart", "Turbo", "Mega" };
	private static readonly string[] ProductNouns = { "Widget", "Gadget", "Gizmo", "Adapter", "Charger", "Cable", "Stand", "Mount", "Case", "Hub", "Keyboard", "Mouse", "Headset", "Speaker", "Monitor", "Webcam", "Dock", "Light", "Pad", "Stylus" };

	public TestingController(AppDbContext db)
	{
		_db = db;
	}

	// ═══════════════════════════════════════════════════════════
	// GENERATE DATA
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Generates a specified number of random test users.
	/// </summary>
	/// <param name="count">Number of users to generate (1-500, default 10).</param>
	/// <returns>Summary of generated users with their IDs.</returns>
	/// <response code="200">Users generated successfully.</response>
	/// <response code="400">Invalid count parameter.</response>
	[HttpPost("users")]
	[ProducesResponseType(typeof(TestingResult), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<TestingResult>> GenerateUsers([FromQuery] int count = 10)
	{
		if (count < 1 || count > 500)
			return BadRequest(new { error = "Count must be between 1 and 500." });

		var users = new List<User>();

		for (int i = 0; i < count; i++)
		{
			var firstName = FirstNames[_rng.Next(FirstNames.Length)];
			var lastName = LastNames[_rng.Next(LastNames.Length)];
			var street = Streets[_rng.Next(Streets.Length)];
			var city = Cities[_rng.Next(Cities.Length)];

			users.Add(new User
			{
				Id = Guid.NewGuid(),
				Name = $"{firstName} {lastName}",
				Email = $"{firstName.ToLower()}.{lastName.ToLower()}.{_rng.Next(1000, 9999)}@test.com",
				Address = $"{street}, {city}, ON",
				CreatedAt = DateTimeOffset.UtcNow.AddDays(-_rng.Next(0, 365))
			});
		}

		_db.Users.AddRange(users);
		await _db.SaveChangesAsync();

		return Ok(new TestingResult
		{
			Message = $"Generated {count} test users.",
			Count = count,
			Ids = users.Select(u => u.Id.ToString()).ToList()
		});
	}

	/// <summary>
	/// Generates a specified number of random test products with varying prices and stock.
	/// </summary>
	/// <param name="count">Number of products to generate (1-500, default 10).</param>
	/// <returns>Summary of generated products.</returns>
	/// <response code="200">Products generated successfully.</response>
	/// <response code="400">Invalid count parameter.</response>
	[HttpPost("products")]
	[ProducesResponseType(typeof(TestingResult), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<TestingResult>> GenerateProducts([FromQuery] int count = 10)
	{
		if (count < 1 || count > 500)
			return BadRequest(new { error = "Count must be between 1 and 500." });

		var products = new List<Product>();

		for (int i = 0; i < count; i++)
		{
			var adj = ProductAdjectives[_rng.Next(ProductAdjectives.Length)];
			var noun = ProductNouns[_rng.Next(ProductNouns.Length)];

			products.Add(new Product
			{
				Name = $"{adj} {noun} {_rng.Next(100, 999)}",
				Price = Math.Round((decimal)(_rng.NextDouble() * 499 + 1), 2),
				StockQty = _rng.Next(5, 500),
				CreatedAt = DateTimeOffset.UtcNow,
				UpdatedAt = DateTimeOffset.UtcNow
			});
		}

		_db.Products.AddRange(products);
		await _db.SaveChangesAsync();

		return Ok(new TestingResult
		{
			Message = $"Generated {count} test products.",
			Count = count,
			Ids = products.Select(p => p.Id.ToString()).ToList()
		});
	}

	/// <summary>
	/// Generates a random order for a user with 1-5 random products from the catalogue.
	/// Creates the order in PENDING state with a PENDING payment and DRAFT items.
	/// </summary>
	/// <param name="userId">Optional user ID. If omitted, picks a random existing user.</param>
	/// <returns>The generated order with items and payment.</returns>
	/// <response code="200">Order generated successfully.</response>
	/// <response code="404">No users or products found — seed them first.</response>
	[HttpPost("orders")]
	[ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Order>> GenerateOrder([FromQuery] Guid? userId = null)
	{
		// Pick or validate user
		User? user;
		if (userId.HasValue)
		{
			user = await _db.Users.FindAsync(userId.Value);
			if (user is null)
				return NotFound(new { error = "Specified user not found." });
		}
		else
		{
			var userCount = await _db.Users.CountAsync();
			if (userCount == 0)
				return NotFound(new { error = "No users in database. Generate users first (POST /api/testing/users)." });

			user = await _db.Users.OrderBy(_ => EF.Functions.Random()).FirstAsync();
		}

		// Pick random products
		var productCount = await _db.Products.CountAsync();
		if (productCount == 0)
			return NotFound(new { error = "No products in database. Generate products first (POST /api/testing/products)." });

		var itemCount = _rng.Next(1, Math.Min(6, productCount + 1));
		var randomProducts = await _db.Products
			.OrderBy(_ => EF.Functions.Random())
			.Take(itemCount)
			.ToListAsync();

		var order = new Order
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			Status = "PENDING",
			CreatedAt = DateTimeOffset.UtcNow
		};

		foreach (var product in randomProducts)
		{
			order.Items.Add(new OrderItem
			{
				Id = Guid.NewGuid(),
				OrderId = order.Id,
				ProductId = product.Id,
				Quantity = _rng.Next(1, 4),
				UnitPrice = product.Price,
				Status = "DRAFT"
			});
		}

		order.Payment = new Payment
		{
			Id = Guid.NewGuid(),
			OrderId = order.Id,
			Status = "PENDING"
		};

		_db.Orders.Add(order);
		await _db.SaveChangesAsync();

		return Ok(order);
	}

	/// <summary>
	/// Generates multiple random orders at once for load/stress testing.
	/// Each order is assigned to a random user with random products.
	/// </summary>
	/// <param name="count">Number of orders to generate (1-200, default 10).</param>
	/// <returns>Summary of generated orders with their IDs.</returns>
	/// <response code="200">Orders generated successfully.</response>
	/// <response code="400">Invalid count parameter.</response>
	/// <response code="404">No users or products found — seed them first.</response>
	[HttpPost("orders/bulk")]
	[ProducesResponseType(typeof(TestingResult), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TestingResult>> GenerateOrdersBulk([FromQuery] int count = 10)
	{
		if (count < 1 || count > 200)
			return BadRequest(new { error = "Count must be between 1 and 200." });

		var users = await _db.Users.ToListAsync();
		if (users.Count == 0)
			return NotFound(new { error = "No users in database. Generate users first (POST /api/testing/users)." });

		var products = await _db.Products.ToListAsync();
		if (products.Count == 0)
			return NotFound(new { error = "No products in database. Generate products first (POST /api/testing/products)." });

		var orderIds = new List<string>();

		for (int i = 0; i < count; i++)
		{
			var user = users[_rng.Next(users.Count)];
			var itemCount = _rng.Next(1, Math.Min(6, products.Count + 1));
			var selectedProducts = products.OrderBy(_ => _rng.Next()).Take(itemCount).ToList();

			var order = new Order
			{
				Id = Guid.NewGuid(),
				UserId = user.Id,
				Status = "PENDING",
				CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-_rng.Next(0, 10080)) // up to 7 days ago
			};

			foreach (var product in selectedProducts)
			{
				order.Items.Add(new OrderItem
				{
					Id = Guid.NewGuid(),
					OrderId = order.Id,
					ProductId = product.Id,
					Quantity = _rng.Next(1, 4),
					UnitPrice = product.Price,
					Status = "DRAFT"
				});
			}

			order.Payment = new Payment
			{
				Id = Guid.NewGuid(),
				OrderId = order.Id,
				Status = "PENDING"
			};

			_db.Orders.Add(order);
			orderIds.Add(order.Id.ToString());
		}

		await _db.SaveChangesAsync();

		return Ok(new TestingResult
		{
			Message = $"Generated {count} test orders.",
			Count = count,
			Ids = orderIds
		});
	}

	// ═══════════════════════════════════════════════════════════
	// PROCESS / SIMULATE
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Processes all pending payments by randomly accepting or declining them.
	/// Accepted payments update the order to PAID and confirm items.
	/// Declined payments update the order to VOID.
	/// </summary>
	/// <param name="acceptRate">Percentage of payments to accept (0-100, default 80).</param>
	/// <returns>Summary of processed payments.</returns>
	/// <response code="200">Payments processed successfully.</response>
	[HttpPost("payments/process")]
	[ProducesResponseType(typeof(TestingResult), StatusCodes.Status200OK)]
	public async Task<ActionResult<TestingResult>> ProcessPendingPayments([FromQuery] int acceptRate = 80)
	{
		acceptRate = Math.Clamp(acceptRate, 0, 100);

		var pendingOrders = await _db.Orders
			.Include(o => o.Items)
			.Include(o => o.Payment)
			.Where(o => o.Status == "PENDING" && o.Payment != null && o.Payment.Status == "PENDING")
			.ToListAsync();

		int accepted = 0, declined = 0;

		foreach (var order in pendingOrders)
		{
			var isAccepted = _rng.Next(100) < acceptRate;

			if (isAccepted)
			{
				// Accept payment and confirm order
				order.Payment!.Status = "ACCEPTED";
				order.Payment.PaidAt = DateTimeOffset.UtcNow;
				order.Status = "PAID";

				foreach (var item in order.Items)
				{
					// Decrement stock
					var product = await _db.Products.FindAsync(item.ProductId);
					if (product != null && product.StockQty >= item.Quantity)
					{
						product.StockQty -= item.Quantity;
						product.UpdatedAt = DateTimeOffset.UtcNow;
						item.Status = "CONFIRMED";
					}
				}
				accepted++;
			}
			else
			{
				// Decline payment and void order
				order.Payment!.Status = "DECLINED";
				order.Status = "VOID";
				declined++;
			}
		}

		await _db.SaveChangesAsync();

		return Ok(new TestingResult
		{
			Message = $"Processed {pendingOrders.Count} pending payments: {accepted} accepted, {declined} declined.",
			Count = pendingOrders.Count
		});
	}

	/// <summary>
	/// Generates a complete order lifecycle: creates an order, accepts payment, and decrements stock in one call.
	/// Useful for quickly populating the database with completed orders.
	/// </summary>
	/// <param name="userId">Optional user ID. If omitted, picks a random existing user.</param>
	/// <returns>The completed order.</returns>
	/// <response code="200">Completed order generated successfully.</response>
	/// <response code="404">No users or products found.</response>
	[HttpPost("orders/completed")]
	[ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Order>> GenerateCompletedOrder([FromQuery] Guid? userId = null)
	{
		// Reuse the generate order logic
		var result = await GenerateOrder(userId);
		if (result.Result is not OkObjectResult okResult || okResult.Value is not Order order)
			return result;

		// Immediately accept payment and confirm items
		if (order.Payment != null)
		{
			order.Payment.Status = "ACCEPTED";
			order.Payment.PaidAt = DateTimeOffset.UtcNow;
		}
		order.Status = "PAID";

		foreach (var item in order.Items)
		{
			var product = await _db.Products.FindAsync(item.ProductId);
			if (product != null && product.StockQty >= item.Quantity)
			{
				product.StockQty -= item.Quantity;
				product.UpdatedAt = DateTimeOffset.UtcNow;
			}
			item.Status = "CONFIRMED";
		}

		await _db.SaveChangesAsync();

		return Ok(order);
	}

	// ═══════════════════════════════════════════════════════════
	// DATABASE STATUS
	// ═══════════════════════════════════════════════════════════

	/// <summary>
	/// Returns the current record counts for all tables.
	/// </summary>
	/// <returns>Record counts per entity.</returns>
	/// <response code="200">Database stats returned.</response>
	[HttpGet("stats")]
	[ProducesResponseType(typeof(DatabaseStats), StatusCodes.Status200OK)]
	public async Task<ActionResult<DatabaseStats>> GetStats()
	{
		return Ok(new DatabaseStats
		{
			Users = await _db.Users.CountAsync(),
			Products = await _db.Products.CountAsync(),
			Orders = await _db.Orders.CountAsync(),
			OrderItems = await _db.OrderItems.CountAsync(),
			Payments = await _db.Payments.CountAsync(),
			PendingOrders = await _db.Orders.CountAsync(o => o.Status == "PENDING"),
			PaidOrders = await _db.Orders.CountAsync(o => o.Status == "PAID"),
			VoidOrders = await _db.Orders.CountAsync(o => o.Status == "VOID")
		});
	}
}

// ─── Response DTOs ──────────────────────────────────────────

/// <summary>Result of a testing/seed operation.</summary>
public class TestingResult
{
	/// <summary>Human-readable summary of the operation.</summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>Number of records affected.</summary>
	public int Count { get; set; }

	/// <summary>IDs of generated records (if applicable).</summary>
	public List<string>? Ids { get; set; }
}

/// <summary>Current database record counts.</summary>
public class DatabaseStats
{
	/// <summary>Total number of users.</summary>
	public int Users { get; set; }
	/// <summary>Total number of products.</summary>
	public int Products { get; set; }
	/// <summary>Total number of orders.</summary>
	public int Orders { get; set; }
	/// <summary>Total number of order items.</summary>
	public int OrderItems { get; set; }
	/// <summary>Total number of payments.</summary>
	public int Payments { get; set; }
	/// <summary>Orders in PENDING status.</summary>
	public int PendingOrders { get; set; }
	/// <summary>Orders in PAID status.</summary>
	public int PaidOrders { get; set; }
	/// <summary>Orders in VOID status.</summary>
	public int VoidOrders { get; set; }
}
