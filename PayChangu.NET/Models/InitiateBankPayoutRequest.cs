using System;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents a request to initiate a PayChangu bank payout.
/// </summary>
public sealed class InitiateBankPayoutRequest
{
    /// <summary>
    /// Gets or sets the payout method.
    /// </summary>
    /// <remarks>
    /// The default payout method is "bank_transfer".
    /// </remarks>
    [JsonPropertyName("payout_method")]
    public string PayoutMethod { get; private set; } = "bank_transfer";

    /// <summary>
    /// Gets or sets the UUID of the recipient's bank or payout provider.
    /// </summary>
    [JsonPropertyName("bank_uuid")]
    public Guid BankId { get; set; }

    /// <summary>
    /// Gets or sets the payout amount.
    /// </summary>
    /// <remarks>
    /// The amount is serialized as a JSON string when sent to PayChangu.
    /// </remarks>
    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the payout request.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recipient's bank account name.
    /// </summary>
    [JsonPropertyName("bank_account_name")]
    public string BankAccountName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recipient's bank account number.
    /// </summary>
    /// <remarks>
    /// For Airtel Money and TNM Mpamba payouts, this can be the recipient's
    /// mobile phone number.
    /// </remarks>
    [JsonPropertyName("bank_account_number")]
    public string BankAccountNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recipient's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the recipient's first name.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the recipient's last name.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
}