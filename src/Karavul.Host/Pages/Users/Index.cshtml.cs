using Karavul.Core.Entities;
using Karavul.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Users;

public class IndexModel : PageModel
{
    private readonly AuthService _authService;

    public IndexModel(AuthService authService)
    {
        _authService = authService;
    }

    public IEnumerable<User> UsersList { get; set; } = new List<User>();

    public async Task<IActionResult> OnGetAsync()
    {
        UsersList = await _authService.GetAllUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        // Kendisini silmesini engelleyebiliriz (opsiyonel)
        if (HttpContext.Session.GetString("UserId") == id)
        {
            TempData["Error"] = "Kendinizi silemezsiniz.";
            return RedirectToPage();
        }

        await _authService.DeleteUserAsync(id);
        TempData["Success"] = "Kullanıcı silindi.";
        return RedirectToPage();
    }
}
