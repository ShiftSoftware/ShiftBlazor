using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Services
{
    public class AppSetting
    {
        public string DateTimeFormat = "yyyy-MM-dd";
        public LanguageInfo? CurrentLanguage { get; set; }
        public int? ListPageSize { get; set; }
        public DialogPosition ModalPosition { get; set; } = DialogPosition.Center;
        public MaxWidth ModalWidth { get; set; } = MaxWidth.Large;
        public Dictionary<string, List<string>> HiddenColumns { get; set; } = new();
    }
}
