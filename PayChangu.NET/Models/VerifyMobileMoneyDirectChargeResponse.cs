using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when verifying a Mobile Money
/// Direct Charge transaction.
/// </summary>
internal sealed class VerifyMobileMoneyDirectChargeResponse
{
    /// <summary>
    /// Gets or sets the API response status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the verified transaction details.
    /// </summary>
    [JsonPropertyName("data")]
    public PayChanguDirectChargeTransaction Data { get; set; } = null!;
}
