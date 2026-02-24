using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Services;
using System.Text.Json;

namespace frontend.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly ApiService _api;

        public IndexModel(ApiService api)
        {
            _api = api;
        }

        // ── Result display ──
        public string? ResultMessage { get; set; }
        public bool IsSuccess { get; set; }
        public string ActiveTab { get; set; } = "users";

        // ── GET data ──
        public List<UserRow>? Users { get; set; }
        public List<ProductRow>? Products { get; set; }
        public List<OrderRow>? Orders { get; set; }
        public string? OptionsResult { get; set; }

        // ── User fields ──
        [BindProperty] public string? CreateUserName { get; set; }
        [BindProperty] public string? CreateUserEmail { get; set; }
        [BindProperty] public string? CreateUserAddress { get; set; }
        [BindProperty] public string? PatchUserId { get; set; }
        [BindProperty] public string? PatchUserName { get; set; }
        [BindProperty] public string? DeleteUserId { get; set; }

        // ── Product fields ──
        [BindProperty] public string? NewProductName { get; set; }
        [BindProperty] public decimal NewProductPrice { get; set; }
        [BindProperty] public int NewProductStock { get; set; }
        [BindProperty] public int AdjustPriceProductId { get; set; }
        [BindProperty] public decimal AdjustPriceValue { get; set; }
        [BindProperty] public int AdjustStockProductId { get; set; }
        [BindProperty] public int AdjustStockValue { get; set; }
        [BindProperty] public int ReplaceProductId { get; set; }
        [BindProperty] public string? ReplaceProductName { get; set; }
        [BindProperty] public decimal ReplaceProductPrice { get; set; }
        [BindProperty] public int ReplaceProductStock { get; set; }
        [BindProperty] public int DeleteProductId { get; set; }

        // ── Order fields ──
        [BindProperty] public string? UpdateOrderId { get; set; }
        [BindProperty] public string? UpdateOrderStatus { get; set; }
        [BindProperty] public string? DeleteOrderId { get; set; }

        public IActionResult OnGet()
        {
            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            if (!string.Equals(isAdmin, "true", StringComparison.OrdinalIgnoreCase))
                return RedirectToPage("/AdminAuth/Login");
            return Page();
        }

        // ═══════════ GET HANDLERS ═══════════

        public async Task<IActionResult> OnPostGetUsers()
        {
            ActiveTab = "users";
            var response = await _api.GetRawAsync("/api/Users");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Users = JsonSerializer.Deserialize<List<UserRow>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                ResultMessage = $"Loaded {Users.Count} users (HTTP {(int)response.StatusCode})";
                IsSuccess = true;
            }
            else await SetResult(response, "Load users", "users");
            return Page();
        }

        public async Task<IActionResult> OnPostGetProducts()
        {
            ActiveTab = "products";
            var response = await _api.GetRawAsync("/api/Products");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Products = JsonSerializer.Deserialize<List<ProductRow>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                ResultMessage = $"Loaded {Products.Count} products (HTTP {(int)response.StatusCode})";
                IsSuccess = true;
            }
            else await SetResult(response, "Load products", "products");
            return Page();
        }

        public async Task<IActionResult> OnPostGetOrders()
        {
            ActiveTab = "orders";
            var response = await _api.GetRawAsync("/api/Orders");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Orders = JsonSerializer.Deserialize<List<OrderRow>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                ResultMessage = $"Loaded {Orders.Count} orders (HTTP {(int)response.StatusCode})";
                IsSuccess = true;
            }
            else await SetResult(response, "Load orders", "orders");
            return Page();
        }

        // ═══════════ OPTIONS ═══════════

        public async Task<IActionResult> OnPostOptions()
        {
            ActiveTab = "products";
            var response = await _api.OptionsAsync("/api/Products");
            var allow = response.Content.Headers.Allow.Count > 0
                ? string.Join(", ", response.Content.Headers.Allow)
                : "(Allow header not returned)";
            OptionsResult = allow;
            ResultMessage = $"OPTIONS response — Allow: {allow} (HTTP {(int)response.StatusCode})";
            IsSuccess = response.IsSuccessStatusCode;
            return Page();
        }

        // ═══════════ USER TOOLS ═══════════

        /// <summary>POST — Create user</summary>
        public async Task<IActionResult> OnPostCreateUser()
        {
            var payload = new { Name = CreateUserName, Email = CreateUserEmail, Address = CreateUserAddress };
            var response = await _api.PostAsync("/api/Users", payload);
            await SetResult(response, "User created", "users");
            return Page();
        }

        /// <summary>PATCH — Update user name</summary>
        public async Task<IActionResult> OnPostUpdateUserName()
        {
            var payload = new { Name = PatchUserName };
            var response = await _api.PatchAsync($"/api/Users/{PatchUserId}", payload);
            await SetResult(response, "User name updated", "users");
            return Page();
        }

        /// <summary>DELETE — Delete user</summary>
        public async Task<IActionResult> OnPostDeleteUser()
        {
            var response = await _api.DeleteAsync($"/api/Users/{DeleteUserId}");
            await SetResult(response, "User deleted", "users");
            return Page();
        }

        // ═══════════ PRODUCT TOOLS ═══════════

        /// <summary>POST — Add product</summary>
        public async Task<IActionResult> OnPostAddProduct()
        {
            var payload = new { Name = NewProductName, Price = NewProductPrice, StockQty = NewProductStock };
            var response = await _api.PostAsync("/api/Products", payload);
            await SetResult(response, "Product added", "products");
            return Page();
        }

        /// <summary>PUT — Adjust price (fetches product, then PUTs with new price)</summary>
        public async Task<IActionResult> OnPostAdjustPrice()
        {
            ActiveTab = "products";
            // GET the product first to keep other fields unchanged
            var existing = await _api.GetRawAsync($"/api/Products/{AdjustPriceProductId}");
            if (!existing.IsSuccessStatusCode)
            {
                await SetResult(existing, "Adjust price failed — product not found", "products");
                return Page();
            }

            var json = await existing.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<ProductRow>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (product == null)
            {
                ResultMessage = "Could not read product data";
                IsSuccess = false;
                return Page();
            }

            var payload = new { Id = AdjustPriceProductId, Name = product.Name, Price = AdjustPriceValue, StockQty = product.StockQty };
            var response = await _api.PutAsync($"/api/Products/{AdjustPriceProductId}", payload);
            await SetResult(response, $"Price updated to ${AdjustPriceValue:F2}", "products");
            return Page();
        }

        /// <summary>PATCH — Adjust stock quantity</summary>
        public async Task<IActionResult> OnPostAdjustStock()
        {
            var payload = new { StockQty = AdjustStockValue };
            var response = await _api.PatchAsync($"/api/Products/{AdjustStockProductId}/stock", payload);
            await SetResult(response, $"Stock updated to {AdjustStockValue}", "products");
            return Page();
        }

        /// <summary>PUT — Replace product entirely</summary>
        public async Task<IActionResult> OnPostReplaceProduct()
        {
            var payload = new { Id = ReplaceProductId, Name = ReplaceProductName, Price = ReplaceProductPrice, StockQty = ReplaceProductStock };
            var response = await _api.PutAsync($"/api/Products/{ReplaceProductId}", payload);
            await SetResult(response, "Product replaced", "products");
            return Page();
        }

        /// <summary>DELETE — Delete product</summary>
        public async Task<IActionResult> OnPostDeleteProduct()
        {
            var response = await _api.DeleteAsync($"/api/Products/{DeleteProductId}");
            await SetResult(response, "Product deleted", "products");
            return Page();
        }

        // ═══════════ ORDER TOOLS ═══════════

        /// <summary>PUT — Update order status</summary>
        public async Task<IActionResult> OnPostUpdateOrderStatus()
        {
            var payload = new { Status = UpdateOrderStatus };
            var response = await _api.PutAsync($"/api/Orders/{UpdateOrderId}", payload);
            await SetResult(response, $"Order status updated to '{UpdateOrderStatus}'", "orders");
            return Page();
        }

        /// <summary>DELETE — Delete order</summary>
        public async Task<IActionResult> OnPostDeleteOrder()
        {
            var response = await _api.DeleteAsync($"/api/Orders/{DeleteOrderId}");
            await SetResult(response, "Order deleted", "orders");
            return Page();
        }

        // ═══════════ HELPERS ═══════════

        private async Task SetResult(HttpResponseMessage response, string successMsg, string tab)
        {
            ActiveTab = tab;
            IsSuccess = response.IsSuccessStatusCode;
            if (IsSuccess)
            {
                ResultMessage = $"{successMsg} (HTTP {(int)response.StatusCode})";
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                ResultMessage = $"Error (HTTP {(int)response.StatusCode}): {body}";
            }
        }

        // ── DTOs ──
        public class UserRow
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string? Address { get; set; }
        }

        public class ProductRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public int StockQty { get; set; }
        }

        public class OrderRow
        {
            public string Id { get; set; } = "";
            public string? UserId { get; set; }
            public string? Status { get; set; }
            public List<OrderItemRow>? Items { get; set; }
            public decimal Total => Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
        }

        public class OrderItemRow
        {
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}
