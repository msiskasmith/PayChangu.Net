using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when verifying a Mobile Money
/// Direct Charge transaction.
/// </summary>
internal sealed class VerifyMobileMoneyDirectChargeResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the verified transaction details.
    /// </summary>
    [JsonPropertyName("data")]
    public PayChanguTransaction Data { get; set; } = null!;
}
