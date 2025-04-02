using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShiftSoftware.ShiftBlazor.Components.FileExplorerNew;

namespace ShiftSoftware.ShiftBlazor.Services;

public class FileExplorerSettings
{
    public FileView View { get; set; }
    public FileSort Sort { get; set; }
    public bool SortDescending { get; set; }
}
