using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Pages.Admin
{

    public class IndexModel : PageModel
    {
        private readonly ApiService _api;

        public IndexModel(ApiService api)
        {
            _api = api;
        }
        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToPage("/AdminAuth/Login");
            }

            return Page();
        }

        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }

        public async Task OnGetAsync()
        {
            var users = await _api.GetAsync<List<object>>("/api/Users");
            var orders = await _api.GetAsync<List<object>>("/api/Orders");

            TotalUsers = users?.Count ?? 0;
            TotalOrders = orders?.Count ?? 0;
        }
    }
}
