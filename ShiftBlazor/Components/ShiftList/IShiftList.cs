using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;

namespace ShiftSoftware.ShiftBlazor.Components;

public interface IShiftList : IODataRequest, IStandaloneComponent, ISortableComponent, IFilterableComponent
{
    public Type? ComponentType { get; set; }
    public Dictionary<string, object>? AddDialogParameters { get; set; }
    public bool EnableSelection { get; set; }
    public bool EnableVirtualization { get; set; }
    public bool Dense { get; set; }
    public bool ShowIDColumn { get; set; }
    public bool Outlined { get; set; }
    public bool IsEmbed { get; }
    public int? PageSize { get; set; }
    public int RowsPerPage { get; }
    public int CurrentPage { get; }

    public HashSet<Guid> ActiveOperations { get; set; }

    public Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, object>? parameters = null);
    public void Reload();
    public int GetItemsCount();
    public Task SetRowsPerPageAsync(int size, bool resetPage = true);
    public void NavigateTo(Page page);
}
