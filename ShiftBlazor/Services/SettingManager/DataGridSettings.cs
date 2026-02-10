using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ShiftBlazor.Services;

public class DataGridSettings
{
    public List<ColumnState>? ColumnStates { get; set; }
    public int? PageSize { get; set; }
    public bool? IsFilterPanelOpen { get; set; }
}
