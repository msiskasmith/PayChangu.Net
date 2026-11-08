using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when initiating a card charge.
/// </summary>
public sealed class InitiateCardDirectChargeResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the charge request succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 3DS authentication is required.
    /// </summary>
    [JsonPropertyName("requires_3ds_auth")]
    public bool Requires3DSAuth { get; set; }

    /// <summary>
    /// Gets or sets the order reference.
    /// </summary>
    [JsonPropertyName("orderReference")]
    public string OrderReference { get; set; } = null!;

    /// <summary>
    /// Gets or sets the 3DS authentication URL.
    /// </summary>
    [JsonPropertyName("3ds_auth_link")]
    public string? ThreeDSAuthLink { get; set; }
}