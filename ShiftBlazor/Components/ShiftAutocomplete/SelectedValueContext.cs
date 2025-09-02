using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Components.ShiftAutocomplete;

public class SelectedValueContext<T> where T : ShiftEntityDTOBase
{
    public ShiftEntitySelectDTO Item { get; private set; }
    public int Index { get; private set; }
    public bool IsHighlighted { get; private set; }
    public string ClassName { get; private set; }
    private readonly ShiftAutocomplete<T> ShiftAutocomplete;

    public SelectedValueContext(ShiftAutocomplete<T> shiftAutocomplete, ShiftEntitySelectDTO item)
    {
        Item = item;
        ShiftAutocomplete = shiftAutocomplete;
        Index = shiftAutocomplete.SelectedValues?.IndexOf(item) ?? -1;
        IsHighlighted = shiftAutocomplete.SelectedValuesIndex == Index;
        ClassName = IsHighlighted ? ShiftAutocomplete<T>.HighlightedClassname : string.Empty;
    }

    public async Task OpenItem()
    {
        await ShiftAutocomplete.AddEditItem(Item.Value);
    }

    public async Task RemoveItem()
    {
        await ShiftAutocomplete.RemoveSelected(Item);
    }

    public async Task HighlightItem()
    {
        await ShiftAutocomplete.ChangeSelectedValuesIndex(Index);
    }
}
