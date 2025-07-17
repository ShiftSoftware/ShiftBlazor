using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class BooleanFilterModel : FilterModelBase
{
    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);

        if (Value != null)
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
