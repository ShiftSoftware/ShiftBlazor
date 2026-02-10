using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Services;

public class AppSetting
{
    public virtual string? DateFormat { get => field; internal set => field = value; }
    public virtual string? TimeFormat { get => field; internal set => field = value; }
    public virtual int? GlobalListPageSize { get => field; internal set => field = value; }
    public virtual DialogPosition? ModalPosition { get => field; internal set => field = value; }
    public virtual MaxWidth? ModalWidth { get => field; internal set => field = value; }
    public virtual FormOnSaveAction? FormOnSaveAction { get => field; internal set => field = value; }
    public virtual bool? EnableFormClone { get => field; internal set => field = value; }
    public virtual bool? IsDrawerOpen { get => field; internal set => field = value; }
}

public static class DefaultAppSetting
{
    public readonly static string DateFormat = "yyyy-MM-dd";
    public readonly static string TimeFormat = "HH:mm";
    public readonly static int ListPageSize = 10;
    public readonly static DialogPosition ModalPosition = DialogPosition.Center;
    public readonly static MaxWidth ModalWidth = MaxWidth.Large;
    public readonly static FormOnSaveAction FormOnSaveAction = FormOnSaveAction.ViewFormOnSave;
    public readonly static Dictionary<string, List<string>> HiddenColumns = [];
    public readonly static LanguageInfo Language = new()
    {
        CultureName = "en-US",
        Label = "English",
        RTL = false,
    };
    public readonly static bool EnableFormClone = false;
    public readonly static FileExplorerSettings FileExplorerSettings = new()
    {
        View = FileView.Detailed,
        Sort = FileSort.Date,
        SortDescending = true,
    };
    public readonly static bool IsDrawerOpen = false;
    public readonly static bool IsDataGridFilterPanelOpen = false;
}
