using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Interfaces;
public interface ISortableComponent
{
    public Task SetSortAsync(string field, SortDirection sortDirection);
    public void SetSort(string field, SortDirection sortDirection);
}
