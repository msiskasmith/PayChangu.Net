using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when verifying a Bank Transfer
/// Direct Charge transaction.
/// </summary>
internal sealed class ApiVerifyBankTransferDirectChargeResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the response data.
    /// </summary>
    [JsonPropertyName("data")]
    public BankTransferDirectChargeVerificationData Data { get; set; } = null!;
}

/// <summary>
/// Represents the data container returned by Bank Transfer verification.
/// </summary>
internal sealed class BankTransferDirectChargeVerificationData
{
    /// <summary>
    /// Gets or sets the verified transaction details.
    /// </summary>
    [JsonPropertyName("transaction")]
    public PayChanguTransaction Transaction { get; set; } = null!;
}
