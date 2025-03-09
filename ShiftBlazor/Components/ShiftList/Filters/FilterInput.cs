using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

public class FilterInput : ComponentBase
{
    [Parameter]
    public string Name { get; set; }

    [CascadingParameter]
    public IShiftList? ShiftList { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();

    private bool Immediate { get; set; } = true;

    public void SetFilter(params ODataFilter[] filters)
    {
        var filter = new ODataFilterGenerator(true, Id);

        foreach (var f in filters)
        {
            filter.Add(f);
        }

        ShiftList?.Filters.Add(filter);
        if (Immediate) ShiftList?.Reload();
    }
}
