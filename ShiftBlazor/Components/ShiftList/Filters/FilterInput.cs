using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

public class FilterInput : ComponentBase
{
    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public bool Immediate { get; set; }

    [CascadingParameter]
    public IShiftList? ShiftList { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();

    private bool ImmediateUpdate => Immediate || ShiftList?.FilterImmediate == true;
    public string ClassName => $"filter-input {this.GetType().Name.ToLower().Replace("filter", "")}-filter";

    public void SetFilter(params ODataFilter[] filters)
    {
        var filter = new ODataFilterGenerator(true, Id);

        foreach (var f in filters)
        {
            filter.Add(f);
        }

        ApplyFilter(filter);
    }

    public void SetFilter(params string[] filters)
    {
        var filter = new ODataFilterGenerator(true, Id);

        foreach (var f in filters)
        {
            filter.Add(f);
        }

        ApplyFilter(filter);
    }

    private void ApplyFilter(ODataFilterGenerator filter)
    {
        ShiftList?.Filters.Add(filter);
        if (ImmediateUpdate) ShiftList?.Reload();
    }

    public void ClearFilter()
    {
        ShiftList?.Filters.Remove(Id);
        if (ImmediateUpdate) ShiftList?.Reload();
    }
}
