using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class StringFilterModel : FilterModelBase
{
    public Type? DtoType { get; set; }

    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);
        var hasValue = !(Value == null || Value is string value && string.IsNullOrWhiteSpace(value));

        if (hasValue || hasValue && (Operator == Enums.ODataOperator.IsEmpty || Operator == Enums.ODataOperator.IsNotEmpty))
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
