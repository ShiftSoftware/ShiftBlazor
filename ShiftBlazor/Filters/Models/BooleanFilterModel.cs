using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class BooleanFilterModel : FilterModelBase
{
    public override bool HasValue()
    {
        return Value != null;
    }

    public override object? ParseValue(object? obj)
    {
        return bool.TryParse(obj?.ToString(), out var value) ? value : null;
    }

    public override string? ValueToString()
    {
        return Value?.ToString();
    }

    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);

        if (this.HasValue())
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = ODataOperator.Equal,
                Value = Value,
                Prefix = Prefix,
                IsCollection = IsCollection,
            });
        }

        return filter;
    }
}
