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
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd";
        public LanguageInfo CurrentLanguage { get; set; } = SettingManager.DefaultLanguage;
        public int ListPageSize { get; set; } = 10;
        public DialogPosition ModalPosition { get; set; } = DialogPosition.Center;
        public MaxWidth ModalWidth { get; set; } = MaxWidth.Large;
        public FormOnSaveAction FormOnSaveAction { get; set; } = FormOnSaveAction.ViewFormOnSave;
        public Dictionary<string, List<string>> HiddenColumns { get; set; } = new();
    }
}
