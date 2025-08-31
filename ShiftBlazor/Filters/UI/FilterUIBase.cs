using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Filters.UI;

public class FilterUIBase : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public FilterModelBase Filter { get; set; }

    [CascadingParameter]
    public IFilterableComponent? Parent { get; set; }

    [CascadingParameter(Name = FormHelper.ParentDisabledName)]
    public bool ParentDisabled { get; set; }

    public string ClassName => $"filter-input {this.GetType().Name.ToLower().Replace("filter", "")}-filter";
    public string MenuIcon => IsMenuOpen ? Icons.Material.Filled.ArrowDropUp : Icons.Material.Filled.ArrowDropDown;
    public Guid Id => Filter.Id;

    public string? Label => Filter.UIOptions.Label ?? Filter.Field;

    protected bool IsMenuOpen = false;
    private Debouncer Debouncer = new();

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

    protected virtual void OperatorChanged(ODataOperator oDataOperator)
    {
        IsMenuOpen = false;
        Filter!.Operator = oDataOperator;

        if (Filter.Value != null)
        {
            UpdateFilter();
        }
    }

    protected void UpdateFilter()
    {
        Debouncer.Debounce(100, Update);
    }

    private void Update()
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

    protected void OnMenuOpened(bool value)
    {
        IsMenuOpen = value;
    }
}
