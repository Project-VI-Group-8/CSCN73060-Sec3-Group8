// Combined Login & Sign Up page with Bootstrap tabs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using frontend.Helpers;
using frontend.Models;
using frontend.Services;
using System.Text.Json;

namespace frontend.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApiService _api;

        public LoginModel(ApiService api)
        {
            _api = api;
        }

        // Login tab fields
        [BindProperty]
        public string LoginEmail { get; set; } = string.Empty;

        // Sign Up tab fields
        [BindProperty]
        public string SignUpName { get; set; } = string.Empty;

        [BindProperty]
        public string SignUpEmail { get; set; } = string.Empty;

        [BindProperty]
        public string SignUpAddress { get; set; } = string.Empty;

        /// <summary>Which tab to show: "login" or "signup"</summary>
        public string ActiveTab { get; set; } = "login";

        /// <summary>True if any user/admin session is active.</summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>True if admin session is active.</summary>
        public bool IsAdmin { get; set; }

        public void OnGet(string? tab)
        {
            if (tab == "signup")
                ActiveTab = "signup";

            IsAdmin = HttpContext.Session.GetString(SessionKeys.IsAdmin) == "true";
            IsLoggedIn = IsAdmin || !string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeys.UserId));
        }

        /// <summary>Handle the Log In form submission.</summary>
        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(LoginEmail))
            {
                ModelState.AddModelError("", "Email is required.");
                ActiveTab = "login";
                return Page();
            }

            var users = await _api.GetAsync<List<UserDto>>("/api/users");
            var user = users?.FirstOrDefault(u =>
                u.Email.Equals(LoginEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                ModelState.AddModelError("", "No account found with that email. Please sign up first.");
                ActiveTab = "login";
                return Page();
            }

            HttpContext.Session.LoginUser(user.Id.ToString(), user.Name, user.Email);
            return RedirectToPage("/Index");
        }

        /// <summary>Handle the Sign Up form submission.</summary>
        public async Task<IActionResult> OnPostSignUpAsync()
        {
            if (string.IsNullOrWhiteSpace(SignUpName) ||
                string.IsNullOrWhiteSpace(SignUpEmail) ||
                string.IsNullOrWhiteSpace(SignUpAddress))
            {
                ModelState.AddModelError("", "All fields are required.");
                ActiveTab = "signup";
                return Page();
            }

            var response = await _api.PostAsync("/api/users", new
            {
                Name = SignUpName.Trim(),
                Email = SignUpEmail.Trim(),
                Address = SignUpAddress.Trim()
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var userId = doc.RootElement.GetProperty("id").GetGuid();
                var userName = doc.RootElement.GetProperty("name").GetString() ?? SignUpName;
                var userEmail = doc.RootElement.GetProperty("email").GetString() ?? SignUpEmail;

                HttpContext.Session.LoginUser(userId.ToString(), userName, userEmail);
                return RedirectToPage("/Index");
            }
            else if ((int)response.StatusCode == 409)
            {
                ModelState.AddModelError("", "An account with that email already exists. Please log in instead.");
                ActiveTab = "signup";
                return Page();
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Registration failed: {errorBody}");
                ActiveTab = "signup";
                return Page();
            }
        }
    }
}
