using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Components;

namespace ShiftSoftware.ShiftBlazor.Filters.UI;

public class FilterUIBase : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public FilterModelBase Filter { get; set; }

    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    public string ClassName => $"filter-input {this.GetType().Name.ToLower().Replace("filter", "")}-filter";
    public Guid Id => Filter.Id;

    protected override void OnInitialized()
    {
        if (Filter == null)
        {
            throw new ArgumentNullException(nameof(Filter));
        }
    }

    protected void ValueChanged<T>(T value)
    {
        Filter!.Value = value;
        UpdateFilter();
    }

    protected void OperatorChanged(ODataOperator oDataOperator)
    {
        Filter!.Operator = oDataOperator;
        UpdateFilter();
    }

    protected void UpdateFilter()
    {
        if (Parent != null)
        {
            Parent.Filters.Remove(Id);
            Parent.Filters.TryAdd(Id, Filter!);
            if (!ReloadList())
            {
                StateHasChanged();
            }
        }
    }

    private bool ReloadList(bool immediate = false)
    {
        if (Parent is IShiftList list && (Parent?.FilterImmediate == true || immediate || Filter.IsImmediate))
        {
            list.Reload();
            return true;
        }

        return false;
    }
}
