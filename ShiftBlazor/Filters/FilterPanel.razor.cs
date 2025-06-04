using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ShiftBlazor.Filters;

public partial class FilterPanel: ComponentBase
{
    [Parameter]
    public Type? DTO { get; set; }

    [Parameter]
    public RenderFragment? FilterTempalte { get; set; }

    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    public ODataFilterGenerator? ODataFilters { get; private set; }

    private IEnumerable<PropertyInfo> Fields = [];
    private bool IsAnd = true;

    protected override void OnInitialized()
    {
        ODataFilters = new ODataFilterGenerator(IsAnd);
    }

    private void RemoveFilterComponent(Guid id)
    {
        ClearFilter(id, true);
    }

    public void ClearFilter(Guid id, bool immediate)
    {
        Parent?.Filters.Remove(id);
        StateHasChanged();
        ReloadList(immediate);
    }

    private void ReloadList(bool immediate)
    {
        if (Parent is IShiftList list && (Parent?.FilterImmediate == true || immediate)) list.Reload();
    }

    private void Reset()
    {
        foreach (var filter in Parent.Filters.Values.Where(x => !x.IsHidden))
        {
            filter.Value = null;
        }
        ReloadList(true);
    }
}
