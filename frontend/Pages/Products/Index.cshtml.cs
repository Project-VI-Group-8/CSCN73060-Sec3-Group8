// Product listing page - displays all available products
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;

namespace frontend.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly ApiService _api;

        public IndexModel(ApiService api)
        {
            _api = api;
        }

        public List<ProductDto> Products { get; set; } = new();

        public async Task OnGetAsync()
        {
            var result = await _api.GetAsync<List<ProductDto>>("/api/Products");
            Products = result ?? new List<ProductDto>();
        }

        public class ProductDto
        {
            public int id { get; set; }
            public string name { get; set; } = "";
            public decimal price { get; set; }
            public int stockQty { get; set; }
        }
    }
}
