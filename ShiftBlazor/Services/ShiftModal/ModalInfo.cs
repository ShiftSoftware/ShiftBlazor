using System.Text.Json.Serialization;

namespace ShiftSoftware.ShiftBlazor.Services;

public class ModalInfo
{
    public string Name { get; set; } = default!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Key { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Parameters { get; set; } = default!;
}
