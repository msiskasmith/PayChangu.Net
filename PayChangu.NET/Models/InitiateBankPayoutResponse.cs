using PayChangu.NET.Json.Converters;
using PayChangu.NET.Models.Base;
using System;
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
    public BankPayoutTransaction Transaction { get; set; } = null!;
}


/// <summary>
/// Represents a PayChangu bank payout transaction.
/// </summary>
public sealed class BankPayoutTransaction
{
    /// <summary>
    /// Gets or sets the payout amount.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PayChanguDecimalJsonConverter))]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the payout charge identifier.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PayChangu reference identifier.
    /// </summary>
    [JsonPropertyName("ref_id")]
    public string ReferenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PayChangu transaction identifier.
    /// </summary>
    [JsonPropertyName("trans_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the transaction currency.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

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

    /// <summary>
    /// Gets or sets the recipient's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction trace identifier.
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mobile number associated with the transaction.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of transaction attempts.
    /// </summary>
    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the transaction mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction creation date and time.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the transaction completion date and time.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the transaction event type.
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction charges.
    /// </summary>
    [JsonPropertyName("transaction_charges")]
    public PayChanguTransactionCharges TransactionCharges { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recipient's bank account details.
    /// </summary>
    [JsonPropertyName("recipient_account_details")]
    public BankRecipientAccountDetails RecipientAccountDetails { get; set; } = null!;
}
