using Karavul.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages;

public class ChangePasswordModel : PageModel
{
    private readonly AuthService _authService;

    public ChangePasswordModel(AuthService authService)
    {
        _authService = authService;
    }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToPage("/Login");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Login");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ErrorMessage = "Şifre en az 6 karakter olmalıdır.";
            return Page();
        }

        if (newPassword != confirmPassword)
        {
            ErrorMessage = "Şifreler eşleşmiyor.";
            return Page();
        }

        await _authService.ChangePasswordAsync(userId, newPassword);
        HttpContext.Session.SetString("PasswordChangeRequired", "false");

        TempData["Success"] = "Şifreniz başarıyla güncellendi.";
        return RedirectToPage("/Index");
    }
}
