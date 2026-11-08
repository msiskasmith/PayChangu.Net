using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when retrieving the details
/// of a PayChangu Mobile Money Direct Charge transaction.
/// </summary>
public sealed class GetMobileMoneyChargeDetailsResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the retrieved transaction details.
    /// </summary>
    [JsonPropertyName("data")]
    public PayChanguTransaction Data { get; set; } = null!;
}