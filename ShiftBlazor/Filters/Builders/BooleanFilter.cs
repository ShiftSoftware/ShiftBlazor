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
            parameters.TryGetValue(nameof(Value), out bool? newValue);

            if (Value != newValue)
            {
                Filter!.Value = newValue;
                HasChanged = true;
            }
        }

        return base.SetParametersAsync(parameters);
    }

}
