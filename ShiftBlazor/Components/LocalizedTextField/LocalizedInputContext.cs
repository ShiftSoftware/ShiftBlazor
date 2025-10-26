using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Components;

public class LocalizedInputContext
{
    public bool Error { get; set; }
    public string? ErrorText { get; set; }
    public string? Value { get; set; }
    public Func<string, Task> ValueChanged { get; set; }
    public string? Label { get; set; }

}
