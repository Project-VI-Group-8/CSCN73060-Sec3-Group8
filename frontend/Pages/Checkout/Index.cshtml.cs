// Checkout page - collect shipping and payment information to complete order
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Helpers;
using frontend.Models;
using frontend.Services;
using System.Text.Json;

namespace frontend.Pages.Checkout
{
    public class IndexModel : PageModel
    {
        private readonly ApiService _api;

        public IndexModel(ApiService api)
        {
            _api = api;
        }

        [BindProperty]
        public string CustomerName { get; set; } = string.Empty;

        [BindProperty]
        public string CustomerEmail { get; set; } = string.Empty;

        [BindProperty]
        public string CustomerAddress { get; set; } = string.Empty;

        /// <summary>True when the user is already logged in via session.</summary>
        public bool IsLoggedIn { get; set; }

        public void OnGet()
        {
            // Pre-fill form if user is logged in
            var userIdStr = HttpContext.Session.GetString(SessionKeys.UserId);
            if (!string.IsNullOrEmpty(userIdStr))
            {
                IsLoggedIn = true;
                CustomerName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "";
                CustomerEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(CustomerName) ||
                string.IsNullOrEmpty(CustomerEmail) ||
                string.IsNullOrEmpty(CustomerAddress))
            {
                ModelState.AddModelError("", "All fields are required.");
                return Page();
            }

            // 1. Read the cart from session
            var cart = HttpContext.Session.GetObject<List<CartItem>>("cart") ?? new List<CartItem>();
            if (!cart.Any())
            {
                ModelState.AddModelError("", "Your cart is empty.");
                return Page();
            }

            // 2. Resolve user ID — use session if logged in, otherwise create/find via API
            Guid userId;
            var sessionUserId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (!string.IsNullOrEmpty(sessionUserId))
            {
                userId = Guid.Parse(sessionUserId);
            }
            else
            {
                // Create the user (or find existing if email is taken)
                var createUserResponse = await _api.PostAsync("/api/users", new
                {
                    Name = CustomerName,
                    Email = CustomerEmail,
                    Address = CustomerAddress
                });

                if (createUserResponse.IsSuccessStatusCode)
                {
                    var json = await createUserResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    userId = doc.RootElement.GetProperty("id").GetGuid();
                }
                else if ((int)createUserResponse.StatusCode == 409)
                {
                    var users = await _api.GetAsync<List<UserDto>>("/api/users");
                    var existing = users?.FirstOrDefault(u =>
                        u.Email.Equals(CustomerEmail, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        ModelState.AddModelError("", "Could not find existing user account.");
                        return Page();
                    }

                    userId = existing.Id;
                }
                else
                {
                    var errorBody = await createUserResponse.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Failed to create user: {errorBody}");
                    return Page();
                }

                // Auto-login the user for future requests
                HttpContext.Session.LoginUser(userId.ToString(), CustomerName, CustomerEmail);
            }

            // 3. Create the order
            var orderItems = cart.Select(c => new
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity
            }).ToList();

            var createOrderResponse = await _api.PostAsync("/api/orders", new
            {
                UserId = userId,
                Items = orderItems
            });

            if (!createOrderResponse.IsSuccessStatusCode)
            {
                var errorBody = await createOrderResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to create order: {errorBody}");
                return Page();
            }

            // 4. Extract the order ID from the response
            var orderJson = await createOrderResponse.Content.ReadAsStringAsync();
            using var orderDoc = JsonDocument.Parse(orderJson);
            var orderId = orderDoc.RootElement.GetProperty("id").GetGuid();

            // 5. Confirm payment so items move from DRAFT → CONFIRMED
            await _api.PostAsync($"/api/orders/{orderId}/confirm-payment", new
            {
                PaymentToken = "checkout-auto"
            });

            // 6. Clear the cart and redirect with the order ID
            HttpContext.Session.Remove("cart");

            return RedirectToPage("/Checkout/Success", new { orderId = orderId.ToString() });
        }
    }
}
