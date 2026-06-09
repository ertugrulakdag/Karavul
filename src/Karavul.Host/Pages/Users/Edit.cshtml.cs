using Karavul.Core.Enums;
using Karavul.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Users;

public class EditModel : PageModel
{
    private readonly AuthService _authService;

    public EditModel(AuthService authService)
    {
        _authService = authService;
    }

    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public List<UserRole> SelectedRoles { get; set; } = new();

    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Users/Index");

        var user = await _authService.GetUserByIdAsync(Id);
        if (user == null) return RedirectToPage("/Users/Index");

        Username = user.Username;
        UpdatedAt = user.UpdatedAt;
        UpdatedBy = user.UpdatedBy;

        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            if (user.Role.HasFlag(role))
            {
                SelectedRoles.Add(role);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Id)) return RedirectToPage("/Users/Index");

        var user = await _authService.GetUserByIdAsync(Id);
        if (user == null) return RedirectToPage("/Users/Index");
        Username = user.Username;

        if (!SelectedRoles.Any())
        {
            TempData["Error"] = "En az bir rol seçmelisiniz.";
            return Page();
        }

        // Eğer kişi kendisini düzenliyorsa ve Admin rolünü kendinden alıyorsa engelle (kendini kitlememesi için)
        var currentUserId = HttpContext.Session.GetString("UserId");
        if (currentUserId == Id && !SelectedRoles.Contains(UserRole.Admin))
        {
            TempData["Error"] = "Kendi hesabınızdan Admin yetkisini kaldıramazsınız.";
            return Page();
        }

        UserRole combinedRole = 0;
        foreach (var role in SelectedRoles)
        {
            combinedRole |= role;
        }

        var currentUsername = HttpContext.Session.GetString("Username") ?? "System";
        var result = await _authService.UpdateUserRoleAsync(Id, combinedRole, currentUsername);
        if (result)
        {
            TempData["Success"] = "Kullanıcı rolü güncellendi.";
            // Eğer kendi yetkilerini güncellediyse Session'daki UserRole da güncellenmeli.
            if (currentUserId == Id)
            {
                HttpContext.Session.SetInt32("UserRole", (int)combinedRole);
            }
            return RedirectToPage("/Users/Index");
        }

        TempData["Error"] = "Güncelleme sırasında bir hata oluştu.";
        return Page();
    }
}
