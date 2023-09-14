namespace ShiftSoftware.ShiftBlazor.Components
{
    public class SelectedItems<T>
    {
        public bool All { get; set; }
        public List<T>? Items { get; set; }
        public string? Query { get; set; }
    }
}
