using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class BooleanFilterModel : FilterModelBase
{
    public override ODataFilterGenerator ToODataFilter()
    {
        return new ODataFilterGenerator(true, Id).Add(new ODataFilter
        {
            Field = Field,
            Operator = ODataOperator.Equal,
            Value = Value is bool val && val == true,
        });
    }
}
