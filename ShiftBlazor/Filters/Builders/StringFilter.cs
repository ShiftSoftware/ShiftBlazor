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
    public string? Value { get; set; }

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo, DTOType, true);
        filter.Value = Value;
        return filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (HasInitialized)
        {
            parameters.TryGetValue(nameof(Value), out string? newValue);

            if (Value != newValue)
            {
                Filter!.Value = newValue;
                HasChanged = true;
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
