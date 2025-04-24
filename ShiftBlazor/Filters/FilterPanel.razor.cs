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
    private bool Immediate;

    protected override void OnInitialized()
    {
        ODataFilters = new ODataFilterGenerator(IsAnd);

        if (DTO != null)
        {
            var fields = DTO.GetProperties().Where(x => x.CanWrite);
            var attr = Misc.GetAttribute<FilterableAttribute>(DTO);
            Immediate = attr?.Immediate ?? false;

            if (attr?.Disabled == true) return;

            // only get fields that have the filterable attribute
            // unless the dto itself has the filterable attribute
            // then get every field that isn't disabled using filterable attribute
            if (attr == null)
            {
                Fields = fields.Where(x => x.GetCustomAttribute<FilterableAttribute>()?.Disabled == false);
            }
            else
            {
                Fields = fields.Where(x => x.GetCustomAttribute<FilterableAttribute>()?.Disabled != true);
            }
        }
    }

    private void AddFilter(PropertyInfo field)
    {
        var filter = FilterModelBase.CreateFilter(field, DTO);
        Parent?.Filters.TryAdd(filter.Id, filter);
        ReloadList(false);
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
        if (Parent is IShiftList list && (Parent?.FilterImmediate == true || immediate || Immediate)) list.Reload();
    }
}
