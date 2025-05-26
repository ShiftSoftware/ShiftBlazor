using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Builders;
using ShiftSoftware.ShiftBlazor.Filters.Models;

namespace ShiftSoftware.ShiftBlazor.Components;

public class ForeignFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public string? EntitySet { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public string? DataValueField { get; set; }

    [Parameter]
    public string? DataTextField { get; set; }

    [Parameter]
    public string? ForeignTextField { get; set; }

    [Parameter]
    public string? ForeignEntiyField { get; set; }

    [Parameter]
    public Type? DTOType { get; set; }

    protected override FilterModelBase CreateFilter(PropertyInfo propertyInfo)
    {
        var filter = FilterModelBase.CreateFilter(propertyInfo, DTOType, true);
        if (filter is StringFilterModel stringFilter)
        {
            stringFilter.AutocompleteOptions = new()
            {
                BaseUrl = BaseUrl,
                BaseUrlKey = BaseUrlKey,
                EntitySet = EntitySet,
                DataTextField = DataTextField,
                DataValueField = DataValueField,
                ForeignTextField = ForeignTextField,
                ForeignEntiyField = ForeignEntiyField,
            };
        }
        return filter;
    }

}
