using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

/// <summary>
/// Manages product catalog operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all products
    /// </summary>
    /// <returns>List of all products</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        return await _context.Products
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                StockQty = p.StockQty,
                HasImage = p.ImageData != null,
                ImageMimeType = p.ImageMimeType
            })
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a specific product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        return ToDto(product);
    }
    
    /// <summary>
    /// Returns the image for a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Image file</returns>
    [HttpGet("{id}/image")]
    public async Task<IActionResult> GetProductImage(int id)
    {
        var product = await _context.Products
            .Where(p => p.Id == id)
            .Select(p => new { p.ImageData, p.ImageMimeType })
            .FirstOrDefaultAsync();

        if (product == null) return NotFound(new { message = $"Product with ID {id} not found" });
        if (product.ImageData == null) return NotFound(new { message = $"Product {id} has no image" });
        return File(product.ImageData, product.ImageMimeType!);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="product">Product details</param>
    /// <returns>Created product</returns>
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return BadRequest(new { message = "Product name is required" });
        }

        if (product.Price <= 0)
        {
            return BadRequest(new { message = "Price must be greater than zero" });
        }

        if (product.StockQty < 0)
        {
            return BadRequest(new { message = "Stock quantity cannot be negative" });
        }

        product.CreatedAt = DateTimeOffset.UtcNow;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="product">Updated product details</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        if (id != product.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return BadRequest(new { message = "Product name is required" });
        }

        if (product.Price <= 0)
        {
            return BadRequest(new { message = "Price must be greater than zero" });
        }

        if (product.StockQty < 0)
        {
            return BadRequest(new { message = "Stock quantity cannot be negative" });
        }

        var existingProduct = await _context.Products.FindAsync(id);
        if (existingProduct == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        existingProduct.Name = product.Name;
        existingProduct.Price = product.Price;
        existingProduct.StockQty = product.StockQty;
        existingProduct.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Updates product stock quantity
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Stock update request</param>
    /// <returns>Updated product</returns>
    [HttpPatch("{id}/stock")]
    public async Task<ActionResult<Product>> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        if (request.StockQty < 0)
        {
            return BadRequest(new { message = "Stock quantity cannot be negative" });
        }

        product.StockQty = request.StockQty;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return product;
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Returns allowed HTTP methods for the Products resource
    /// </summary>
    /// <returns>200 OK with Allow header</returns>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, PUT, PATCH, DELETE, OPTIONS");
        return Ok();
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
    
    /// <summary>
    /// Maps a Product entity to a ProductDto
    /// </summary>
    private static ProductDto ToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQty = product.StockQty,
            HasImage = product.ImageData != null,
            ImageMimeType = product.ImageMimeType
        };
    }
}

/// <summary>
/// Request model for updating product stock
/// </summary>
public class UpdateStockRequest
{
    /// <summary>
    /// New stock quantity
    /// </summary>
    public int StockQty { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQty { get; set; }
    public bool HasImage { get; set; }
    public string? ImageMimeType { get; set; }    
}