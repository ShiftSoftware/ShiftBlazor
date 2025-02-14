using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public virtual bool? IsDrawerOpen { get; set; }
    }

    public static class DefaultAppSetting
    {
        public static string DateFormat = "yyyy-MM-dd";
        public static string TimeFormat = "HH:mm";
        public static int ListPageSize = 10;
        public static DialogPosition ModalPosition = DialogPosition.Center;
        public static MaxWidth ModalWidth = MaxWidth.Large;
        public static FormOnSaveAction FormOnSaveAction = FormOnSaveAction.ViewFormOnSave;
        public static Dictionary<string, List<string>> HiddenColumns = new();
        public static LanguageInfo Language = new LanguageInfo
        {
            CultureName = "en-US",
            Label = "English",
            RTL = false,
        };
        public static bool EnableFormClone = false;
        public static bool IsDrawerOpen = false;
    }
}
