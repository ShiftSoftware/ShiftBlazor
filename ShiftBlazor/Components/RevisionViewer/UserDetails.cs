using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components;

public class UserDetails : ShiftEntityListDTO
{
    [ShiftEntity.Model.HashIds.UserHashIdConverter]
    public override string? ID { get; set; }

    public string Name { get; set; }
}
