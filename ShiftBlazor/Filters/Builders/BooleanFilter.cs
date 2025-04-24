using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class BooleanFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public bool Value { get; set; }

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo);
        filter.Value = Value;
        return filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (IsInitialized)
        {
            var newValue = parameters.GetValueOrDefault<bool>(nameof(Value));

            if (Value != newValue)
            {
                UpdateFilterValue(newValue);
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
