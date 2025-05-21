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
    public DateRange? Value { get; set; }
    [Parameter]
    public TProperty? DateTimeValue { get; set; }
    [Parameter]
    public TimeUnit TimeUnit { get; set; } = TimeUnit.Day;
    [Parameter]
    public int UnitValue { get; set; } = 1;

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo, isDefault: true);
        var dateFilter = filter as DateFilterModel;

        if (dateFilter != null)
        {
            // DateTimeFilter builder was refactored, variable names need to be updated
            dateFilter.Value = DateTimeValue;
            dateFilter.SelectedDateOperator = DateFilterOperator.Range;
            dateFilter.SelectedTimeUnit = TimeUnit;
            dateFilter.UnitValue = UnitValue;
            dateFilter.DateRangeValue = Value;
        }

        return dateFilter ?? filter;
    }

}
