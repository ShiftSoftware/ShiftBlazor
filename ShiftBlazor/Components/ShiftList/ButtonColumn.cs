using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

public class ButtonColumn<T, TProperty> : PropertyColumnExtended<T, TProperty>
    where T : ShiftEntityDTOBase, new()
{
    protected override void OnInitialized()
    {
        IsButtonColumn = true;
        base.OnInitialized();
    }
}
