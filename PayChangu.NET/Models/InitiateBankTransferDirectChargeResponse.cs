using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when initiating a Bank Transfer Direct Charge.
/// </summary>
public sealed class InitiateBankTransferDirectChargeResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the bank transfer charge details.
    /// </summary>
    [JsonPropertyName("data")]
    public BankTransferDirectChargeData Data { get; set; } = null!;
}

/// <summary>
/// Represents the data returned when initiating a Bank Transfer Direct Charge.
/// </summary>
public sealed class BankTransferDirectChargeData
{
    /// <summary>
    /// Gets or sets the bank account details for the payment.
    /// </summary>
    [JsonPropertyName("payment_account_details")]
    public BankTransferPaymentAccountDetails PaymentAccountDetails { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction details.
    /// </summary>
    [JsonPropertyName("transaction")]
    public PayChanguTransaction Transaction { get; set; } = null!;
}

/// <summary>
/// Represents the bank account details provided for a Bank Transfer Direct Charge.
/// </summary>
public sealed class BankTransferPaymentAccountDetails
{
    /// <summary>
    /// Gets or sets the name of the bank.
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string BankName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the bank account number.
    /// </summary>
    [JsonPropertyName("account_number")]
    public string AccountNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the account holder.
    /// </summary>
    [JsonPropertyName("account_name")]
    public string AccountName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Unix timestamp indicating when the bank account expires.
    /// </summary>
    [JsonPropertyName("account_expiration_timestamp")]
    public long AccountExpirationTimestamp { get; set; }
}