using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.Builders;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Components;

public class BooleanFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public bool? Value { get; set; }

    [Obsolete]
    public new ODataOperator Operator { get; set; }

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo, isDefault: true);
        filter.Value = Value;
        return filter;
    }

}
