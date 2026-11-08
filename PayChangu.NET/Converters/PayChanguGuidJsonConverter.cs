using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Json.Converters;

/// <summary>
/// Converts JSON strings to GUID values, defaulting to Guid.Empty
/// when the supplied value is invalid.
/// </summary>
public sealed class PayChanguGuidJsonConverter : JsonConverter<Guid>
{
    /// <inheritdoc />
    public override Guid Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();

            return Guid.TryParse(value, out var guid)
                ? guid
                : Guid.Empty;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return Guid.Empty;
        }

        throw new JsonException("The JSON value could not be converted to a GUID.");
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        Guid value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}