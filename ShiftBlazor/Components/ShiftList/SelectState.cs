using ShiftSoftware.ShiftEntity.Model.Dtos;


namespace ShiftSoftware.ShiftBlazor.Components;

public class SelectState<T> : SelectStateDTO<T>
{
    public void Clear()
    {
        Items.Clear();
        All = false;
        Filter = null;
    }
}
