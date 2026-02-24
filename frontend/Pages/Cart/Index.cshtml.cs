// Shopping cart page - displays items added to cart and allows removal
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Helpers;
using frontend.Models;

namespace frontend.Pages.Cart
{
    public class IndexModel : PageModel
    {
        public List<CartItem> CartItems { get; set; } = new();

        public void OnGet()
        {
            CartItems = HttpContext.Session.GetObject<List<CartItem>>("cart") ?? new List<CartItem>();
        }

        public IActionResult OnPostRemove(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("cart") ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item != null)
                cart.Remove(item);

            HttpContext.Session.SetObject("cart", cart);
            return RedirectToPage();
        }

        public IActionResult OnPostClear()
        {
            HttpContext.Session.Remove("cart");
            return RedirectToPage();
        }

        public decimal GetTotal()
        {
            return CartItems.Sum(x => x.Price * x.Quantity);
        }
    }
}
