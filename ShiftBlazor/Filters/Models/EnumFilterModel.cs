using System.Collections;
using System.Text.Json;
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
        //Console.WriteLine($"{Operator} {JsonSerializer.Serialize(Value)}");

        if (Value != null && Value is IEnumerable<object> val && val.Any())
        {
            filter.Add(new ODataFilter
            {
                Field = Field,
                Operator = Operator == ODataOperator.NotIn ? Operator : ODataOperator.In,
                Value = Value
            });
        }

        //if (Value != null && Enum.IsDefined(Value.GetType(), Value))
        //{
        //    filter.Add(new ODataFilter
        //    {
        //        Field = Field,
        //        Operator = ODataOperator.Equal,
        //        Value = Value
        //    });
        //}

        return filter;
    }
}
