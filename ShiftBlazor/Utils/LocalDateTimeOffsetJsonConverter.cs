using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ShiftBlazor.Utils;

public class LocalDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return DateTimeOffset.Parse(reader.GetString()!).ToLocalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset dateTimeValue, JsonSerializerOptions options)
    {
        writer.WriteStringValue(dateTimeValue.ToString("O"));
    }
}