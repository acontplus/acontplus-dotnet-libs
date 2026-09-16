namespace Acontplus.Utilities.Time;

/// <summary>
/// Converter for nullable DateTime values
/// </summary>
/// <param name="dateFormat">The date format string used for serialization. Defaults to "yyyy-MM-dd".</param>
public class NullableDateTimeConverter(string dateFormat = "yyyy-MM-dd") : JsonConverter<DateTime?>
{
    private readonly string _dateFormat = dateFormat;

    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return null;
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
        }
        return reader.TokenType == JsonTokenType.Null ? null : throw new JsonException("Invalid date format.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(_dateFormat, CultureInfo.InvariantCulture));
        else
            writer.WriteNullValue();
    }
}
