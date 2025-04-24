using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Filters;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class DateTimeFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public TProperty? Value { get; set; }
    [Parameter]
    public DateFilterOperator DateOperator { get; set; }
    [Parameter]
    public TimeUnit TimeUnit { get; set; } = TimeUnit.Day;
    [Parameter]
    public int UnitValue { get; set; } = 1;
    [Parameter]
    public DateRange? DateRangeValue { get; set; }

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo);
        var dateFilter = filter as DateFilterModel;

        if (dateFilter != null)
        {
            dateFilter.Value = Value;
            dateFilter.SelectedDateOperator = DateOperator;
            dateFilter.SelectedTimeUnit = TimeUnit;
            dateFilter.UnitValue = UnitValue;
            dateFilter.DateRangeValue = DateRangeValue;
        }

        return dateFilter ?? filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (IsInitialized)
        {
            var newValue = parameters.GetValueOrDefault<TProperty>(nameof(Value));
            var newDateOperator = parameters.GetValueOrDefault<DateFilterOperator>(nameof(DateOperator));
            var newTimeUnit = parameters.GetValueOrDefault<TimeUnit>(nameof(TimeUnit));
            var newUnitValue = parameters.GetValueOrDefault<int>(nameof(UnitValue));
            var newDateRangeValue = parameters.GetValueOrDefault<DateRange?>(nameof(DateRangeValue));


            if (!EqualityComparer<TProperty>.Default.Equals(Value, newValue) ||
                DateOperator != newDateOperator ||
                TimeUnit != newTimeUnit ||
                UnitValue != newUnitValue ||
                DateRangeValue != newDateRangeValue)
            {
                if (Filter is DateFilterModel filter)
                {
                    filter.Value = newValue ?? default!;
                    filter.SelectedDateOperator = newDateOperator;
                    filter.SelectedTimeUnit = newTimeUnit;
                    filter.UnitValue = newUnitValue;
                    filter.DateRangeValue = newDateRangeValue;
                }
            }

            if (Value?.Equals(newValue) == false)
            {
                UpdateFilterValue(newValue);
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
