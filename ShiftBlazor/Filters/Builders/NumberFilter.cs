using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class NumberFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public TProperty Value { get; set; }

    [Parameter]
    public TProperty Value2 { get; set; }

    [Parameter]
    public double PercentValue { get; set; }

    [Parameter]
    public NumericFilterOperator? NumberOperator { get; set; }

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo);
        SetNumberFilterValues(filter as NumericFilterModel);
        return filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (IsInitialized)
        {
            // check if the parameters have changed
            var newValue = parameters.GetValueOrDefault<TProperty>(nameof(Value));
            var newValue2 = parameters.GetValueOrDefault<TProperty>(nameof(Value2));
            var newPercentValue = parameters.GetValueOrDefault<double>(nameof(PercentValue));
            var newNumberOperator = parameters.GetValueOrDefault<NumericFilterOperator?>(nameof(NumberOperator));

            if (!EqualityComparer<TProperty>.Default.Equals(Value, newValue) ||
                !EqualityComparer<TProperty>.Default.Equals(Value2, newValue2) ||
                PercentValue != newPercentValue ||
                NumberOperator != newNumberOperator)
            {
                if (Filter is NumericFilterModel filter)
                {
                    filter.Value = newValue ?? default!;
                    filter.Value2 = newValue2 ?? default!;
                    filter.PercentValue = newPercentValue;
                    filter.SelectedNumOperator = newNumberOperator;
                }
            }

        }
        return base.SetParametersAsync(parameters);
    }

    private void SetNumberFilterValues(NumericFilterModel? filter)
    {
        if (filter != null)
        {
            filter.Value = Value;
            filter.Value2 = Value2;
            filter.PercentValue = PercentValue;
            filter.SelectedNumOperator = NumberOperator;
        }
    }
}
