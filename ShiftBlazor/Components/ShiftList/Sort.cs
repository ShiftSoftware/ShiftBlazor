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

    private string PropertyPath = string.Empty;

    protected override void OnInitialized()
    {
        if (SortableComponent != null)
        {
            PropertyPath = Misc.GetExpressionPath(Property);
            //_ = SortableComponent.SetSortAsync(PropertyPath, Direction);
            //SortableComponent.SetSort(PropertyPath, Direction);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // This is required duo to a bug that will show "0"
        // instead of "1" next to the sort icon in MudDataGrid
        // which is only visual and doesn't effect the grid
        if (firstRender && SortableComponent != null)
        {
            SortableComponent.SetSort(PropertyPath, Direction);

            //_ = SortableComponent.SetSortAsync(PropertyPath, Direction);
        }
    }
}
