using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class NumberFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public TProperty? Value { get; set; }

    protected override FilterModelBase CreateFilter(string path, Type propertyType)
    {
        var filter = FilterModelBase.CreateFilter(path, propertyType, isDefault: true);
        filter.Value = Value;
        return filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (HasInitialized)
        {
            parameters.TryGetValue(nameof(Value), out TProperty? newValue);

            if (Value?.Equals(newValue) == false)
            {
                Filter!.Value = newValue;
                HasChanged = true;
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
