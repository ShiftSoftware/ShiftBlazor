using System.Reflection;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Builders;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Components;

public class ForeignFilter<T, TProperty> : FilterBuilder<T, TProperty>
{
    [Parameter]
    public IEnumerable<string>? Value { get; set; }
    private IEnumerable<string>? OldValue { get; set; }

    [Parameter]
    public string? EntitySet { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public Dictionary<string, object>? AdditionalParameters { get; set; }

    [Parameter]
    public Type? DTOType { get; set; }

    protected override FilterModelBase CreateFilter(string path, Type propertyType)
    {
        var filter = FilterModelBase.CreateFilter(path, propertyType, DTOType, true);
        
        Operator ??= ODataOperator.In;
        filter.Value = Value; 

        if (filter is StringFilterModel stringFilter)
        {
            stringFilter.AutocompleteOptions = new()
            {
                BaseUrl = BaseUrl,
                BaseUrlKey = BaseUrlKey,
                EntitySet = EntitySet,
                AdditionalParameters = AdditionalParameters,
            };
        }
        return filter;
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (HasInitialized)
        {
            foreach (var parameter in parameters)
            {
                var isEqual = parameter.Name switch
                {
                    nameof(BaseUrl) => BaseUrl == parameter.Value as string,
                    nameof(BaseUrlKey) => BaseUrlKey == parameter.Value as string,
                    nameof(EntitySet) => EntitySet == parameter.Value as string,
                    _ => true,
                };

                if (!isEqual)
                {
                    HasChanged = true;
                    break;
                }
                else if (parameter.Name == nameof(Value))
                {
                    var newValue = parameter.Value as IEnumerable<string>;

                    if (!new HashSet<string>(OldValue ?? []).SetEquals(newValue ?? []))
                    {
                        Filter!.Value = newValue;
                        HasChanged = true;
                    }
                    OldValue = newValue?.ToList();

                    break;
                }
            }
        }

        await base.SetParametersAsync(parameters);

        if (Filter is StringFilterModel stringFilter)
        {
            stringFilter.AutocompleteOptions = new()
            {
                BaseUrl = BaseUrl,
                BaseUrlKey = BaseUrlKey,
                EntitySet = EntitySet,
                AdditionalParameters = AdditionalParameters,
            };
        }
    }
}
