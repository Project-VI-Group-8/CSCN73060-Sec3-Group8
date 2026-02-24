// Admin logout - redirects to shared logout which clears all session keys
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages.AdminAuth
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect to the shared logout endpoint
            return RedirectToPage("/Account/Logout");
        }
    }
}
