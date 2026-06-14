using System.Data;
using Dapper;

namespace CabinConnect.Infrastructure.Common;

// Dapper does not natively understand DateOnly; this handler bridges to ADO.NET DbType.Date.
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateTime dt => DateOnly.FromDateTime(dt),
        DateOnly d  => d,
        string s    => DateOnly.Parse(s),
        _           => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateOnly")
    };
}
