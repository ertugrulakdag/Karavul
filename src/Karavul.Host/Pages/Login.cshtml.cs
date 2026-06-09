using Karavul.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.DataProtection;

namespace Karavul.Host.Pages;

public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly Microsoft.AspNetCore.DataProtection.IDataProtector _protector;

    public LoginModel(AuthService authService, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dpProvider)
    {
        _authService = authService;
        _protector = dpProvider.CreateProtector("Karavul.RememberMe");
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString("UserId") != null)
            return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Kullanıcı adı ve şifre gereklidir.";
            return Page();
        }

        var user = await _authService.ValidateLoginAsync(Username, Password);
        if (user == null)
        {
            ErrorMessage = "Geçersiz kullanıcı adı veya şifre.";
            return Page();
        }

        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetInt32("UserRole", (int)user.Role);
        HttpContext.Session.SetString("PasswordChangeRequired",
            user.IsPasswordChangeRequired ? "true" : "false");

        if (RememberMe)
        {
            var protectedId = _protector.Protect(user.Id);
            HttpContext.Response.Cookies.Append("Karavul.RememberMe", protectedId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true
            });
        }
        else
        {
            HttpContext.Response.Cookies.Delete("Karavul.RememberMe");
        }

        if (user.IsPasswordChangeRequired)
            return RedirectToPage("/ChangePassword");

        return RedirectToPage("/Index");
    }
}
