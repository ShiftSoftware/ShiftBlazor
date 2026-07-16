using ShiftSoftware.ShiftEntity.Model.Dtos;


namespace ShiftSoftware.ShiftBlazor.Components;

public class SelectState<T> : SelectStateDTO<T> where T : ShiftEntityDTOBase
{
    // O(1) membership test for per-row rendering. Row templates check selection on EVERY row on
    // EVERY render (row class + checkbox state); scanning Items there is O(rows × selected) per
    // render, which visibly lags large pages. Mutate the selection through the methods below —
    // writing to Items directly bypasses this index.
    private readonly HashSet<string?> ids = [];

    public bool Contains(T item) => ids.Count > 0 && ids.Contains(item.ID);

    /// <summary>Toggles the item: removes it when present, adds it otherwise. Returns true when the item ended up selected.</summary>
    public bool Toggle(T item)
    {
        if (ids.Remove(item.ID))
        {
            Items.RemoveAll(x => x.ID == item.ID);
            return false;
        }

        Items.Add(item);
        ids.Add(item.ID);
        return true;
    }

    public void Clear()
    {
        Items.Clear();
        ids.Clear();
        All = false;
        Filter = null;
    }

    /// <summary>Clears only the selected items (keeps the current Filter), e.g. when toggling select-all.</summary>
    public void ClearItems()
    {
        Items.Clear();
        ids.Clear();
    }
}
