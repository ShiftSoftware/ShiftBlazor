using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class EnumFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public IEnumerable<TProperty>? Value { get; set; }

    private IEnumerable<TProperty>? OldValue { get; set; }

    protected override FilterModelBase CreateFilter(string path, Type propertyType)
    {
        var filter = FilterModelBase.CreateFilter(path, propertyType, isDefault: true);

        Operator ??= ODataOperator.In;

        filter.Value = Value;

        return filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (HasInitialized)
        {
            parameters.TryGetValue(nameof(Value), out IEnumerable<TProperty>? newValue);

            if (!new HashSet<TProperty>(OldValue ?? []).SetEquals(newValue ?? []))
            {
                Filter!.Value = newValue;
                HasChanged = true;
            }

            OldValue = newValue?.ToList();
        }

        return base.SetParametersAsync(parameters);
    }
}
