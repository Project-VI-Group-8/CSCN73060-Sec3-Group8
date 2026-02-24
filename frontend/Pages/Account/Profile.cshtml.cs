// User profile page - view user account information and order history
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Helpers;
using frontend.Services;

namespace frontend.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly ApiService _api;

        public ProfileModel(ApiService api)
        {
            _api = api;
        }

        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public List<OrderSummaryDto> Orders { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToPage("/Account/Login");

            var userId = Guid.Parse(userIdStr);
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "";
            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "";

            // Fetch all orders and filter by this user
            var allOrders = await _api.GetAsync<List<OrderSummaryDto>>("/api/orders");
            if (allOrders != null)
            {
                Orders = allOrders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();
            }

            return Page();
        }
    }

    /// <summary>DTO for deserializing order data from the API.</summary>
    public class OrderSummaryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    /// <summary>DTO for order items.</summary>
    public class OrderItemDto
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
