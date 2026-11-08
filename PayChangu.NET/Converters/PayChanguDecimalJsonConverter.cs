using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Json.Converters;

/// <summary>
/// Converts JSON values to decimal values, defaulting invalid or empty values to zero.
/// </summary>
public sealed class PayChanguDecimalJsonConverter : JsonConverter<decimal>
{
    /// <summary>
    /// Gets a value indicating whether this converter handles JSON null values.
    /// </summary>
    public override bool HandleNull => true;

    /// <summary>
    /// Reads a JSON value and converts it to a decimal.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>
    /// The converted decimal value, or zero if the value is null, empty,
    /// or cannot be converted to a decimal.
    /// </returns>
    public override decimal Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetDecimal(out var number)
                    ? number
                    : 0m;

            case JsonTokenType.String:
                var value = reader.GetString();

                return decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsedValue)
                    ? parsedValue
                    : 0m;

            case JsonTokenType.Null:
                return 0m;

            default:
                return 0m;
        }
    }

    /// <summary>
    /// Writes a decimal value as a JSON number.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The decimal value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        decimal value,
        JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}