using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Filters.Builders;

namespace ShiftSoftware.ShiftBlazor.Components;

public class DateTimeFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    
    [Parameter]
    public DateTime? DateStart { get; set; }

    [Parameter]
    public DateTime? DateEnd { get; set; }

    protected override FilterModelBase CreateFilter(string path, Type propertyType)
    {
        var filter = FilterModelBase.CreateFilter(path, propertyType, isDefault: true);

        if (DateStart != null || DateEnd != null)
        {
            filter.Value = new DateRange(DateStart?.Date, DateEnd?.Date);
        }

        return filter;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (HasInitialized)
        {
            parameters.TryGetValue(nameof(DateStart), out DateTime? newStart);
            parameters.TryGetValue(nameof(DateEnd), out DateTime? newEnd);

            if (DateStart != newStart ||
                DateEnd != newEnd)
            {
                Filter!.Value = new DateRange(DateStart?.Date, DateEnd?.Date);
                HasChanged = true;
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
