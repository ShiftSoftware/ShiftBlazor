using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Services;

public class FileExplorerSettings
{
    public FileView View { get; set; }
    public FileSort Sort { get; set; }
    public bool SortDescending { get; set; }
}
