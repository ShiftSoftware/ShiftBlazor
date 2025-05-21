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
        var filter = FilterModelBase.CreateFilter(propertyInfo, isDefault: true);
        var numericFilter = filter as NumericFilterModel;

        if (numericFilter != null)
        {
            numericFilter.Value = Value == null ? 0d : Value;
            numericFilter.Value2 = Value2 == null ? 0d : Value2;
            numericFilter.PercentValue = PercentValue;
            numericFilter.SelectedNumOperator = NumberOperator;
        }
        return numericFilter ?? filter;
    }

    protected override void OnParametersChanged()
    {
        base.OnParametersChanged();
        if (Filter is NumericFilterModel filter)
        {
            filter.Value = Value;
            filter.Value2 = Value2;
            filter.PercentValue = PercentValue;
            filter.SelectedNumOperator = NumberOperator;
        }
    }
}
