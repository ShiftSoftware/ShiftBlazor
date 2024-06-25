namespace ShiftSoftware.ShiftBlazor.Components;

public class SelectState<T>
{
    public bool All { get; set; }
    public List<T> Items { get; set; } = [];
    public int Count => All ? Total : Items.Count;
    public int Total { get; set; }
    public string? Filter { get; set; }

    public void Clear()
    {
        Items.Clear();
        All = false;
        Filter = null;
    }
}