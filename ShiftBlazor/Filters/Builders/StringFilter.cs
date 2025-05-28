using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class StringFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public Type? DTOType { get; set; }

    [Parameter]
    public string Value { get; set; } = string.Empty;

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo, DTOType, true);
        filter.Value = Value;
        return filter;
    }

    protected override void OnParametersChanged()
    {
        base.OnParametersChanged();
        Filter!.Value = Value;
    }
}
