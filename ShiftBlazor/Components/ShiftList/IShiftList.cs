using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public interface IShiftList
    {
        public Guid Id { get; }
        public string? Title { get; set; }
        public bool? ParentReadOnly { get; set; }
        public bool? ParentDisabled { get; set; }
        public Type? ComponentType { get; set; }
        public Dictionary<string, object>? AddDialogParameters { get; set; }
        public bool EnableSelection { get; set; }
        public bool EnableVirtualization { get; set; }
        public string Height { get; set; }
        public string? NavColor { get; set; }
        public bool NavIconFlatColor { get; set; }
        public string IconSvg { get; set; }
        public bool Dense { get; set; }
        public bool ShowIDColumn { get; set; }
        public int? PageSize { get; set; }
        public bool Outlined { get; set; }
        public bool IsEmbed { get; }

        public Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, object>? parameters = null);

    }
}
