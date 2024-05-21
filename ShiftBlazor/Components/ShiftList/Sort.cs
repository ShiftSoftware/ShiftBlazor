using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Components;

public class Sort<T, TProperty> : ComponentBase where T : ShiftEntityDTOBase, new()
{
    [Parameter]
    [EditorRequired]
    public Expression<Func<T, TProperty>> Property { get; set; }

    [Parameter]
    public SortDirection Direction { get; set; } = SortDirection.Ascending;

    [CascadingParameter]
    public ISortableComponent? SortableComponent { get; set; }

    protected override void OnInitialized()
    {
        if (SortableComponent != null)
        {
            var PropertyPath = Misc.GetExpressionPath(Property);
            SortableComponent.SetSort(PropertyPath, Direction);
        }
    }

}
