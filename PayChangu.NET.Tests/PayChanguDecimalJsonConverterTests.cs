using System.Text.Json;
using PayChangu.NET.Json.Converters;
using Xunit;

namespace PayChangu.NET.Tests.Json.Converters;

public sealed class PayChanguDecimalJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new PayChanguDecimalJsonConverter()
        }
    };

    [Theory]
    [InlineData("610.2", "610.2")]
    [InlineData("\"610.2\"", "610.2")]
    [InlineData("\"\"", "0")]
    [InlineData("\" \"", "0")]
    [InlineData("\"invalid\"", "0")]
    [InlineData("null", "0")]
    [InlineData("-10.5", "-10.5")]
    public void Read_ReturnsExpectedDecimal_WhenJsonValueIsValidOrInvalid(
    string json,
    string expected)
    {
        // Arrange
        var expectedValue = decimal.Parse(
            expected,
            System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = JsonSerializer.Deserialize<decimal>(
            json,
            JsonOptions);

        // Assert
        Assert.Equal(expectedValue, result);
    }
}