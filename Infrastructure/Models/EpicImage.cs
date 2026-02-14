using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VictorNovember.Infrastructure.Models;

public sealed class EpicImage
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;
    [JsonPropertyName("caption")]
    public string Caption { get; set; } = string.Empty;
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;
    [JsonConverter(typeof(EpicDateTimeConverter))]
    public DateTime Date { get; set; }
}

public sealed class EpicDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateTime.ParseExact(value!, Format, CultureInfo.InvariantCulture);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}