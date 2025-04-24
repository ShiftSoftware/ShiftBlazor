using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class StringFilterModel : FilterModelBase
{
    public Type? DtoType { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        return new ODataFilterGenerator(true, Id).Add(new ODataFilter
        {
            Field = Field,
            Operator = Operator,
            Value = Value
        });
    }
}
