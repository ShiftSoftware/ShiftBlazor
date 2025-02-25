using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

public class FilterInput : ComponentBase
{
    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public ODataFilterGenerator Value { get;set; }

    [Parameter]
    public EventCallback<ODataFilterGenerator> ValueChanged { get; set; }
}
