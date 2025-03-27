using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftList.Filters;

public class FilterInput : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public FilterBase Filter { get; set; }

    [Parameter]
    public bool Immediate { get; set; }

    [Parameter]
    public Type? FieldType { get; set; }

    [Parameter]
    public bool IsHidden { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

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
            StateHasChanged();
        }
    }
}
