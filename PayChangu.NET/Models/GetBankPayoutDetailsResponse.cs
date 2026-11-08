using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when retrieving bank payout details.
/// </summary>
public sealed class GetBankPayoutDetailsResponse
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
    /// Gets or sets the bank payout transaction details.
    /// </summary>
    [JsonPropertyName("data")]
    public BankPayoutTransaction Data { get; set; } = null!;
}