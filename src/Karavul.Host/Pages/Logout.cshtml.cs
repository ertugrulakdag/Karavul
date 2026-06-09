using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        HttpContext.Session.Clear();
        HttpContext.Response.Cookies.Delete("Karavul.RememberMe");
        return RedirectToPage("/Login");
    }
}
