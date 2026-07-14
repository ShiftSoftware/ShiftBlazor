using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Globalization;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class NumericFilterModel : FilterModelBase
{
    public override bool HasValue()
    {
        return Value != null;
    }

    public override object? ParseValue(object? obj)
    {
        var str = obj?.ToString();
        if (string.IsNullOrEmpty(str))
            return null;

        return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    public override string? ValueToString()
    {
        return (Value as double?)?.ToString(CultureInfo.InvariantCulture) ?? Value?.ToString();
    }

    public override ODataFilterGenerator ToODataFilter()
    {
        object? value = Value;
        var castType = string.Empty;
        var filter = new ODataFilterGenerator(true, Id);
        var builder = new ODataFilter
        {
            Field = Field,
            Operator = Operator,
            Prefix = Prefix,
            IsCollection = IsCollection,
        };

        if (IsNoValueOperator)
        {
            filter.Add(builder);
        }
        else if (this.HasValue())
        {
            if (Operator is
                ODataOperator.StartsWith or
                ODataOperator.NotStartsWith or
                ODataOperator.EndsWith or
                ODataOperator.NotEndsWith or
                ODataOperator.Contains or
                ODataOperator.NotContains)
            {
                value = Value?.ToString() ?? string.Empty;
                castType = ODataPrimitiveTypes.String;
            }

            builder.Value = value;
            builder.CastToType = castType;
            filter.Add(builder);
        }

        return filter;
    }
}
