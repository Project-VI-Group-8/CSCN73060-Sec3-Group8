// Shared logout - clears all auth session keys (user + admin)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Helpers;

namespace frontend.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.LogoutAll();
            return RedirectToPage("/Index");
        }
    }
}
