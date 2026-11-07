using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when a bank payout is initiated.
/// </summary>
public sealed class InitiateBankPayoutResponse : BasePaychanguResponse
{

    /// <summary>
    /// Gets or sets the payout response data.
    /// </summary>
    [JsonPropertyName("data")]
    public BankPayoutResponseData Data { get; set; } = null!;
}

/// <summary>
/// Represents the data returned for an initiated bank payout.
/// </summary>
public sealed class BankPayoutResponseData
{
    /// <summary>
    /// Gets or sets the bank payout transaction details.
    /// </summary>
    [JsonPropertyName("transaction")]
    public PayChanguPayoutData Transaction { get; set; } = null!;
}