using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class NumericFilterModel : FilterModelBase
{
    public override ODataFilterGenerator ToODataFilter()
    {
        object? value = Value;
        var castType = string.Empty;
        var filter = new ODataFilterGenerator(true, Id);

        if (Value == null && !(Operator == ODataOperator.IsEmpty || Operator == ODataOperator.IsNotEmpty))
        {
            return filter;
        }

        if (Operator is
            ODataOperator.StartsWith or
            ODataOperator.NotStartsWith or
            ODataOperator.EndsWith or
            ODataOperator.NotEndsWith or
            ODataOperator.Contains or
            ODataOperator.NotContains)
        {
            value = Value?.ToString() ?? "";
            castType = ODataPrimitiveTypes.String;
        }

        filter.Add(new ODataFilter
        {
            Field = Field,
            Operator = Operator,
            Value = value,
            CastToType = castType
        });

        return filter;
    }
}
