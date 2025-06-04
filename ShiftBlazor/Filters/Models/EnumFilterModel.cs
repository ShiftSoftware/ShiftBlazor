using System.Collections;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class EnumFilterModel : FilterModelBase
{
    public Type? EnumType { get; set; }
    public Type? EnumTypeToUse
    {
        get
        {
            if (EnumType != null)
            {
                return EnumType.IsEnum ? EnumType : Nullable.GetUnderlyingType(EnumType);
            }
            return null;
        }
    }

    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);

        var val = (Value as IEnumerable)?.Cast<object>();

        if (val != null && val.Any())
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = Operator == ODataOperator.NotIn ? Operator : ODataOperator.In,
                Value = Value
            });
        }

        return filter;
    }
}
