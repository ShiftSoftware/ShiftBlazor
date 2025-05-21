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
        var filter = FilterModelBase.CreateFilter(propertyInfo, isDefault: true);
        var dateFilter = filter as DateFilterModel;

        if (dateFilter != null)
        {
            dateFilter.Value = Value;
            dateFilter.SelectedDateOperator = DateFilterOperator.Range;
            dateFilter.SelectedTimeUnit = TimeUnit;
            dateFilter.UnitValue = UnitValue;
            dateFilter.DateRangeValue = DateRangeValue;
        }

        return dateFilter ?? filter;
    }

}
