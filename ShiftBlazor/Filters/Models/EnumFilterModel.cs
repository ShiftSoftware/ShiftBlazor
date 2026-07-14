using System.Collections;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class EnumFilterModel : FilterModelBase
{
    public Type? EnumType { get; set; }
    public RenderFragment? ChildContent { get; set; }

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

    public override bool HasValue()
    {
        var val = GetAsEnum(Value);
        return val != null && val.Any();
    }

    private IEnumerable<object>? GetAsEnum(object? value)
    {
        return (value as IEnumerable)?.Cast<object>();
    }

    public override object? ParseValue(object? obj)
    {
        var names = Split(obj as string);

        if (names == null || EnumTypeToUse == null)
            return null;

        return names.Select(name => Enum.Parse(EnumTypeToUse, name)).ToList();
    }

    public override string? ValueToString()
    {
        return Join(GetAsEnum(Value));
    }

    public override ODataFilterGenerator ToODataFilter()
    {
        var filter = new ODataFilterGenerator(true, Id);
        var builder = new ODataFilter
        {
            Field = Field,
            Operator = Operator,
            Prefix = Prefix,
            IsCollection = IsCollection,
        };

        if (IsNoValueOperator)
        {
            filter.Add(builder);
        }
        else if (this.HasValue())
        {
            builder.Operator = Operator == ODataOperator.NotIn ? Operator : ODataOperator.In;
            builder.Value = Value;
            filter.Add(builder);
        }

        return filter;
    }
}
