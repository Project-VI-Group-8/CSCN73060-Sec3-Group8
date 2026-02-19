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
        public async Task<IActionResult> OnGetAsync()
        {
            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            if (!String.Equals(isAdmin, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/AdminAuth/Login");
            }
            return Page();
        }
    }
}
