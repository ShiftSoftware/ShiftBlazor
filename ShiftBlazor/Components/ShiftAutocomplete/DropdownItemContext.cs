using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftAutocomplete;

public class DropdownItemContext<T> where T : ShiftEntityDTOBase
{
    public T Item { get; set; } = default!;
    public required string Value { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsHighlighted { get; set; }
    public int Index { get; set; }
}
