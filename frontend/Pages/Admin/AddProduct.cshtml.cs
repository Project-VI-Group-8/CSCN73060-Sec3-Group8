using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;

namespace frontend.Pages.Admin
{
    public class AddProductModel : PageModel
    {
        private readonly ApiService _api;

        public AddProductModel(ApiService api)
        {
            _api = api;
        }

        [BindProperty]
        public string Name { get; set; } = "";

        [BindProperty]
        public decimal Price { get; set; }

        [BindProperty]
        public int StockQty { get; set; }

        public string Message { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            var response = await _api.PostAsync("/api/Products", new
            {
                name = Name,
                price = Price,
                stockQty = StockQty
            });

            if (response.IsSuccessStatusCode)
            {
                Message = "Product added successfully!";
                return Page();
            }

            Message = "Failed to add product.";
            return Page();
        }
    }
}

