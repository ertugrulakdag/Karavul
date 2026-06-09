using Karavul.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace Karavul.Host.Extensions;

public static class AuthExtensions
{
    public static bool HasRole(this HttpContext context, UserRole allowedRoles)
    {
        var roleInt = context.Session.GetInt32("UserRole");
        if (roleInt == null) return false;

        var userRole = (UserRole)roleInt.Value;
        
        if (userRole.HasFlag(UserRole.Admin)) return true;

        return (userRole & allowedRoles) != 0;
    }
}
