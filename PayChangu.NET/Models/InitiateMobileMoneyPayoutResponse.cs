using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the response returned when a mobile money payout is initiated.
/// </summary>
public sealed class InitiateMobileMoneyPayoutResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the mobile money payout transaction details.
    /// </summary>
    [JsonPropertyName("data")]
    public PayChanguPayoutData Data { get; set; } = null!;
}