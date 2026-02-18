using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;

namespace frontend.Pages.Admin
{
    public class OrderSearchModel : PageModel
    {
        private readonly ApiService _api;

        public OrderSearchModel(ApiService api)
        {
            _api = api;
        }

        [BindProperty]
        public int OrderId { get; set; }

        public OrderDto? Order { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            Order = await _api.GetAsync<OrderDto>($"/api/Orders/{OrderId}");

            if (Order == null)
            {
                ModelState.AddModelError("", "Order not found.");
                return Page();
            }

            return Page();
        }

        public class OrderDto
        {
            public int id { get; set; }
            public string status { get; set; } = "";
            public object? user { get; set; }
            public object? items { get; set; }
        }
    }
}

