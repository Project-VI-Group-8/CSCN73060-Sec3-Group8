// Unoptimized product listing - fetches full product blobs from API (slow, for comparison)
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;

namespace frontend.Pages.Products
{
    public class UnoptimizedModel : PageModel
    {
        private readonly ApiService _api;

        public UnoptimizedModel(ApiService api)
        {
            _api = api;
        }

        public List<ProductBlobDto> Products { get; set; } = new();
        public long ElapsedMs { get; set; }

        public async Task OnGetAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await _api.GetAsync<List<ProductBlobDto>>("/api/testing/products/unoptimized");
            sw.Stop();
            ElapsedMs = sw.ElapsedMilliseconds;
            Products = result ?? new List<ProductBlobDto>();
        }

        public class ProductBlobDto
        {
            public int id { get; set; }
            public string name { get; set; } = "";
            public decimal price { get; set; }
            public int stockQty { get; set; }
            public string? imageData { get; set; }
            public string? imageMimeType { get; set; }
        }
    }
}
