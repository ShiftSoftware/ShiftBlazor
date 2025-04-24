using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters.Models;

namespace ShiftSoftware.ShiftBlazor.Components;

public class StringFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public Type? DTOType { get; set; }

    [Parameter]
    public string Value { get; set; } = string.Empty;

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo, DTOType);
        filter.Value = Value;
        return filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (IsInitialized)
        {
            var newValue = parameters.GetValueOrDefault<string>(nameof(Value));

            if (Value != newValue)
            {
                UpdateFilterValue(newValue);
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
