using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

abstract public class FilterInput : ComponentBase
{
    [EditorRequired]
    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public bool Immediate { get; set; }

    [Parameter]
    public Type? FieldType { get; set; }

    [Parameter]
    public bool IsHidden { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Guid Id { get; set; } = Guid.NewGuid();

    [CascadingParameter]
    public FilterPanel? FilterPanel { get; set; }

    public string ClassName => $"filter-input {this.GetType().Name.ToLower().Replace("filter", "")}-filter";

    abstract public void SetODataFilter();

    protected override void OnInitialized()
    {
        var immediate = Immediate;
        Immediate = false;
        SetODataFilter();
        Immediate = immediate;
    }

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
