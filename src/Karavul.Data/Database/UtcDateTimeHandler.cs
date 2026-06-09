using Dapper;
using System.Data;

namespace Karavul.Data.Database;

public class UtcDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value.ToString("o");
    }

    public override DateTime Parse(object value)
    {
        if (value is string s)
            return DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
            
        return Convert.ToDateTime(value).ToUniversalTime();
    }
}

public class NullableUtcDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override void SetValue(IDbDataParameter parameter, DateTime? value)
    {
        parameter.Value = value?.ToString("o");
    }

    public override DateTime? Parse(object value)
    {
        if (value == null || value is DBNull) return null;
        
        if (value is string s)
            return DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
            
        return Convert.ToDateTime(value).ToUniversalTime();
    }
}
