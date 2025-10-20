using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Events;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters;

public partial class FilterPanel : ComponentBase, IDisposable
{
    [Parameter]
    public Type? DTO { get; set; }

    [Parameter]
    public RenderFragment? FilterTempalte { get; set; }

    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    private IShiftList? ShiftList { get; set; }

    public ODataFilterGenerator? ODataFilters { get; private set; }

    private readonly IEnumerable<PropertyInfo> Fields = [];
    private readonly bool IsAnd = true;
    private bool IsLoading { get; set; }

    protected override void OnInitialized()
    {
        ODataFilters = new ODataFilterGenerator(IsAnd);

        // if this component is used inside a ShiftList,
        // we assign it to the global ShiftList variable
        // so we can reload the list when filters change
        // and also subscribe to the OnBeforeGridDataBound event
        if (Parent is IShiftList list)
        {
            ShiftList = list;
            ShiftBlazorEvents.OnBeforeGridDataBound += OnListFetched;
        }
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
        if (ShiftList != null && (Parent?.FilterImmediate == true || immediate))
        {
            IsLoading = true;
            ShiftList.Reload();
        }
    }

    private void OnListFetched(object? sender, KeyValuePair<Guid, List<object>> args)
    {
        if (ShiftList?.Id == args.Key)
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void Reset()
    {
        if (Parent == null)
            return;

        foreach (var filter in Parent.Filters.Values.Where(x => !x.IsHidden))
        {
            filter.Value = null;
        }
        ReloadList(true);
    }

    public void Dispose()
    {
        if (ShiftList != null)
        {
            ShiftBlazorEvents.OnBeforeGridDataBound -= OnListFetched;
        }
        GC.SuppressFinalize(this);
    }
}