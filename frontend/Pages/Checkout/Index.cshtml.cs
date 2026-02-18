// Checkout page - collect shipping and payment information to complete order
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace frontend.Pages.Checkout
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string CustomerName { get; set; }

        [BindProperty]
        public string CustomerEmail { get; set; }

        [BindProperty]
        public string CustomerAddress { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(CustomerName) ||
                string.IsNullOrEmpty(CustomerEmail) ||
                string.IsNullOrEmpty(CustomerAddress))
            {
                ModelState.AddModelError("", "All fields are required.");
                return Page();
            }

            // For now we are not sending order to backend
            // Later you can POST this data to /api/orders endpoint.

            return RedirectToPage("/Checkout/Success");
        }
    }
}

