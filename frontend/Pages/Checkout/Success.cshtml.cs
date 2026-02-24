using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages.Checkout
{
    public class SuccessModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? OrderId { get; set; }

        public void OnGet()
        {
        }
    }
}
