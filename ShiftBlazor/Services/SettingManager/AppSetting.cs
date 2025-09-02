using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class AppSetting
    {
        public virtual string? DateFormat { get; set; }
        public virtual string? TimeFormat { get; set; }
        public virtual int? ListPageSize { get; set; }
        public virtual DialogPosition? ModalPosition { get; set; }
        public virtual MaxWidth? ModalWidth { get; set; }
        public virtual FormOnSaveAction? FormOnSaveAction { get; set; }
        public virtual Dictionary<string, List<ColumnState>>? ColumnStates { get; set; }
        public virtual LanguageInfo? Language { get; set; }
        public virtual bool? EnableFormClone { get; set; }
        public virtual Dictionary<string, FileExplorerSettings>? FileExplorerSettings { get; set; }
        public virtual bool? IsDrawerOpen { get; set; }
        public virtual bool? IsDataGridFilterPanelOpen { get; set; }
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
}
