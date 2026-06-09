using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages;

public class SetLanguageModel : PageModel
{
    public IActionResult OnGet(string lang, string returnUrl = "/")
    {
        if (lang == "tr" || lang == "en")
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddYears(1),
                IsEssential = true,
                Path = "/"
            };
            Response.Cookies.Append("Karavul.Lang", lang, cookieOptions);
        }

        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return Redirect(returnUrl);
    }
}
