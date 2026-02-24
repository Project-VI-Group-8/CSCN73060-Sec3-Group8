// Order details page - shows detailed information about a specific order
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Helpers;
using frontend.Services;

namespace frontend.Pages.Orders
{
    public class DetailsModel : PageModel
    {
        private readonly ApiService _api;

        public DetailsModel(ApiService api)
        {
            _api = api;
        }

        public OrderDetailDto? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            var isAdmin = HttpContext.Session.GetString(SessionKeys.IsAdmin) == "true";

            // Must be logged in as user or admin
            if (string.IsNullOrEmpty(userId) && !isAdmin)
                return RedirectToPage("/Account/Login");

            Order = await _api.GetAsync<OrderDetailDto>($"/api/orders/{id}");

            if (Order == null)
                return RedirectToPage("/Account/Profile");

            // Non-admin users can only view their own orders
            if (!isAdmin && Order.UserId.ToString() != userId)
                return RedirectToPage("/Account/Profile");

            return Page();
        }
    }

    public class OrderDetailDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public List<OrderDetailItemDto> Items { get; set; } = new();
        public OrderPaymentDto? Payment { get; set; }
    }

    public class OrderDetailItemDto
    {
        public Guid Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public ProductInfoDto? Product { get; set; }
    }

    public class ProductInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class OrderPaymentDto
    {
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? PaidAt { get; set; }
    }
}
