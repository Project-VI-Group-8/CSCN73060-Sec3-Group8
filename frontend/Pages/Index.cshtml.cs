using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;

public class IndexModel : PageModel
{
    private readonly ApiService _api;

    public IndexModel(ApiService api)
    {
        _api = api;
    }

    public List<ProductDto> FeaturedProducts { get; set; } = new();

    public async Task OnGetAsync()
    {
        var products = await _api.GetAsync<List<ProductDto>>("/api/Products");

        if (products != null)
        {
            FeaturedProducts = products.Take(8).ToList();
        }
    }

    public class ProductDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public decimal price { get; set; }
    }
}
