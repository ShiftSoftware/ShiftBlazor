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
        return new ODataFilterGenerator(true, Id).Add(new ODataFilter
        {
            Field = Field,
            Operator = ODataOperator.Equal,
            Value = Value
        });
    }
}
