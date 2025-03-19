using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

public class FilterInput : ComponentBase
{
    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public bool Immediate { get; set; }

    [Parameter]
    public Type? FieldType { get; set; }

    [Parameter]
    public Guid Id { get; set; } = Guid.NewGuid();

    [CascadingParameter]
    public FilterPanel? FilterPanel { get; set; }

    public string ClassName => $"filter-input {this.GetType().Name.ToLower().Replace("filter", "")}-filter";

    public void SetFilter(params ODataFilter[] filters)
    {
        var filter = new ODataFilterGenerator(true, Id);

        foreach (var f in filters)
        {
            filter.Add(f);
        }

        FilterPanel?.ApplyFilter(filter, Immediate);
    }

    public void SetFilter(params string[] filters)
    {
        var filter = new ODataFilterGenerator(true, Id);

        foreach (var f in filters)
        {
            filter.Add(f);
        }

        FilterPanel?.ApplyFilter(filter, Immediate);
    }

    public void ClearFilter() => FilterPanel?.ClearFilter(Id, Immediate);
}
