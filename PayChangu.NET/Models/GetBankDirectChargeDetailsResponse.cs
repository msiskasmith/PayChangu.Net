using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when retrieving the details
/// of a PayChangu Bank Transfer Direct Charge transaction.
/// </summary>
public sealed class GetBankDirectChargeDetailsResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the retrieved transaction data.
    /// </summary>
    [JsonPropertyName("data")]
    public BankTransferDirectChargeDetailsData Data { get; set; } = null!;
}

/// <summary>
/// Represents the data containing the details of a
/// PayChangu Bank Transfer Direct Charge transaction.
/// </summary>
public sealed class BankTransferDirectChargeDetailsData
{
    /// <summary>
    /// Gets or sets the retrieved transaction.
    /// </summary>
    [JsonPropertyName("transaction")]
    public PayChanguTransaction Transaction { get; set; } = null!;
}