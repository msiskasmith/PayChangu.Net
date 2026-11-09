using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Json.Converters;

/// <summary>
/// Converts JSON string or numeric values into strings.
/// </summary>
public sealed class PayChanguStringJsonConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override string? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.GetRawText();
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        throw new JsonException(
            $"Cannot convert JSON token '{reader.TokenType}' to a string.");
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        string value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}