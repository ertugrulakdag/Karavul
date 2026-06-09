using Karavul.Core.Enums;
using Karavul.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Users;

public class CreateModel : PageModel
{
    private readonly AuthService _authService;

    public CreateModel(AuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public List<UserRole> SelectedRoles { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            TempData["Error"] = "Kullanıcı adı ve şifre gereklidir.";
            return Page();
        }

        if (!SelectedRoles.Any())
        {
            TempData["Error"] = "En az bir rol seçmelisiniz.";
            return Page();
        }

        UserRole combinedRole = 0;
        foreach (var role in SelectedRoles)
        {
            combinedRole |= role;
        }

        try
        {
            var username = HttpContext.Session.GetString("Username") ?? "System";
            await _authService.CreateUserAsync(Username, Password, combinedRole, username);
            TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
            return RedirectToPage("/Users/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Kullanıcı oluşturulurken bir hata oluştu: " + ex.Message;
            return Page();
        }
    }
}
