using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class EnumFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public TProperty? Value { get; set; }

    [Obsolete]
    public new ODataOperator Operator { get; set; }

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
            var newValue = parameters.GetValueOrDefault<TProperty>(nameof(Value));

            if (Value?.Equals(newValue) == false)
            {
                UpdateFilterValue(newValue);
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
