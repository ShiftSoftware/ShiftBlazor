using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftAutocomplete;

public class AutcompleteInputContext<T> where T : ShiftEntityDTOBase
{
    public bool IsFocused => ShiftAutocomplete.IsFocused;
    public bool IsOpen => ShiftAutocomplete.IsDropdownOpen;
    public bool IsLoading => ShiftAutocomplete.IsLoading;
    public bool IsIntitialValueLoading => ShiftAutocomplete.IsIntitialValueLoading;
    public readonly ShiftAutocomplete<T> ShiftAutocomplete;

    public Func<Task> OnInputFocus { get; init; }
    public Func<Task> OnInputBlur { get; init; }
    public Func<string, Task> OnInputChange { get; init; }

    public AutcompleteInputContext(ShiftAutocomplete<T> shiftAutocomplete)
    {
        ShiftAutocomplete = shiftAutocomplete;
        OnInputFocus = ShiftAutocomplete.InputFocusHandler;
        OnInputBlur = ShiftAutocomplete.InputBlurHandler;
        OnInputChange = ShiftAutocomplete.TextChangedHandler;
    }
}
