using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftAutocomplete;

public class SelectedValuesGroupContext<T> where T : ShiftEntityDTOBase
{
    public RenderFragment Content { get; set; }
    public List<ShiftEntitySelectDTO> SelectedValues { get; init; }
    public Action Open { get; init; }
    public Action Close { get; init; }
    public bool IsOpen => ShiftAutocomplete.IsSelectedValuesGroupOpen;
    public readonly ShiftAutocomplete<T> ShiftAutocomplete;

    public SelectedValuesGroupContext(ShiftAutocomplete<T> shiftAutocomplete)
    {
        ShiftAutocomplete = shiftAutocomplete;
        Open = shiftAutocomplete.OpenSelectedValuesGroup;
        Close = shiftAutocomplete.CloseSelectedValuesGroup;
        Content = shiftAutocomplete.SelectedValuesRenderFragment;
        SelectedValues = shiftAutocomplete.SelectedValues ?? [];
    }
}
