using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Components;

public class FilterComponent<T, TProperty> : ComponentBase where T : ShiftEntityDTOBase, new()
{

    [Parameter]
    [EditorRequired]
    public Expression<Func<T, TProperty>> Property { get; set; }

    [Parameter]
    public ODataOperator Operator { get; set; } = ODataOperator.Equal;

    [Parameter]
    public object? Value { get; set; }

    [Parameter]
    public bool Removable { get; set; } = false;

    [CascadingParameter]
    public IFilterableComponent? FilterableComponent { get; set; }

    private Guid id;

    protected override void OnInitialized()
    {
        id = Guid.NewGuid();
    }

    protected override void OnParametersSet()
    {
        var memberExpression = Property.Body as MemberExpression;
        var fieldName = memberExpression?.Member.Name;
        if (!string.IsNullOrWhiteSpace(fieldName))
        {
            FilterableComponent?.AddFilter(id, fieldName, Operator, Value);
        }
    }
}
