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
    public DateTime? DateStart { get; set; }

    [Parameter]
    public DateTime? DateEnd { get; set; }

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo, isDefault: true);

        filter.Value = new DateRange(DateStart?.Date, DateEnd?.Date);

        return filter;
    }

    protected override void OnParametersChanged()
    {
        base.OnParametersChanged();
        if (Filter!.Value is DateRange dateRange)
        {
            dateRange.Start = DateStart?.Date;
            dateRange.End = DateEnd?.Date;
        }
        else
        {
            Filter!.Value = new DateRange(DateStart?.Date, DateEnd?.Date);
        }
    }
}
