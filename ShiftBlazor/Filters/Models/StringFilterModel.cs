using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class StringFilterModel : FilterModelBase
{
    public Type? DtoType { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);

        if (Value != null || Value == null && (Operator == Enums.ODataOperator.IsEmpty || Operator == Enums.ODataOperator.IsNotEmpty))
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = Operator,
                Value = Value
            });
        }

        return filter;
    }
}
