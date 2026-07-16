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
        // Fast path: ISO-8601, which is what the server writes (see Write below). The reader parses
        // it straight from the UTF-8 buffer; DateTimeOffset.Parse(string) is culture-sensitive and
        // far slower, which adds up at hundreds of rows per response. Non-ISO values fall back.
        if (reader.TryGetDateTimeOffset(out var value))
            return value.ToLocalTime();

        return DateTimeOffset.Parse(reader.GetString()!).ToLocalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset dateTimeValue, JsonSerializerOptions options)
    {
        writer.WriteStringValue(dateTimeValue.ToString("O"));
    }
}