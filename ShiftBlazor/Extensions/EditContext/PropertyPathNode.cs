

namespace ShiftSoftware.ShiftBlazor.Extensions.EditContext;

internal class PropertyPathNode
{
    public PropertyPathNode? Parent { get; set; }
    public object? ModelObject { get; set; }
    public string? PropertyName { get; set; }
    public int? Index { get; set; }
}
