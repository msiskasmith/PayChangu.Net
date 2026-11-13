namespace PayChangu.NET.Models.Base;

/// <summary>
/// Represents a base class for most paychangu responses.
/// </summary>
public class BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
}
