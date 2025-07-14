using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShiftSoftware.ShiftBlazor.Extensions.EditContext;

internal class PropertyPathNode
{
    public PropertyPathNode? Parent { get; set; }
    public object? ModelObject { get; set; }
    public string? PropertyName { get; set; }
    public int? Index { get; set; }
}
