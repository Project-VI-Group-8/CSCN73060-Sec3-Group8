// Product details page - shows detailed information about a single product
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;
using frontend.Helpers;
using frontend.Models;

namespace frontend.Pages.Products
{
    public class DetailsModel : PageModel
    {
        private readonly ApiService _api;

        public DetailsModel(ApiService api)
        {
            _api = api;
        }

        public ProductDto? Product { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Product = await _api.GetAsync<ProductDto>($"/api/Products/{id}");

            if (Product == null)
                return RedirectToPage("/Products/Index");

            return Page();
        }

        public IActionResult OnPostAddToCart(int id, string name, decimal price, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var cart = HttpContext.Session.GetObject<List<CartItem>>("cart") ?? new List<CartItem>();

            var existing = cart.FirstOrDefault(x => x.ProductId == id);

            if (existing != null)
                existing.Quantity += quantity;
            else
                cart.Add(new CartItem
                {
                    ProductId = id,
                    Name = name,
                    Price = price,
                    Quantity = quantity
                });

            HttpContext.Session.SetObject("cart", cart);

            return RedirectToPage("/Cart/Index");
        }

        public class ProductDto
        {
            public int id { get; set; }
            public string name { get; set; } = "";
            public decimal price { get; set; }
            public int stockQty { get; set; }
            public bool hasImage { get; set; }
            public string? imageMimeType { get; set; }
        }
    }
}

