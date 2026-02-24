// Sign up redirect - redirects to the combined Login/SignUp page
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages.Account
{
    public class SignUpModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Account/Login", new { tab = "signup" });
        }
    }
}
