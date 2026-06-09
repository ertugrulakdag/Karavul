using System;

namespace Karavul.Core.Helpers;

public static class GuidEncoder
{
    public static string Encode(Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('='); // Tam olarak 22 karakter üretir
    }

    public static Guid Decode(string code)
    {
        if (string.IsNullOrEmpty(code)) return Guid.Empty;

        string incoming = code.Replace("-", "+").Replace("_", "/");
        switch (code.Length % 4)
        {
            case 2: incoming += "=="; break;
            case 3: incoming += "="; break;
        }
        
        return new Guid(Convert.FromBase64String(incoming));
    }
}
