using PayChangu.NET.Json.Converters;
using PayChangu.NET.Models;
using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents payout transaction details returned by the PayChangu API.
/// </summary>
public sealed class PayChanguPayoutData
{
    /// <summary>
    /// Gets or sets the unique identifier assigned to the payout charge.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reference identifier assigned to the payout.
    /// </summary>
    [JsonPropertyName("ref_id")]
    public string ReferenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider transaction identifier, if available.
    /// </summary>
    [JsonPropertyName("trans_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the currency in which the payout is processed.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payout amount.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PayChanguDecimalJsonConverter))]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the recipient's first name, if provided.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the recipient's last name, if provided.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the recipient's email address, if provided.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the type of transaction.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the provider trace identifier, if available.
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the current status of the payout.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recipient's mobile number, if provided.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// Gets or sets the number of processing attempts.
    /// </summary>
    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the environment in which the payout was processed.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the payout was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout was completed, if available.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the event type associated with the payout, if provided.
    /// </summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets the charges associated with the payout, if provided.
    /// </summary>
    [JsonPropertyName("transaction_charges")]
    public PayChanguTransactionCharges? TransactionCharges { get; set; }

    /// <summary>
    /// Gets or sets the recipient's bank account details, if applicable.
    /// </summary>
    [JsonPropertyName("recipient_account_details")]
    public BankRecipientAccountDetails? RecipientAccountDetails { get; set; }

    /// <summary>
    /// Gets or sets the mobile money operator details, if applicable.
    /// </summary>
    [JsonPropertyName("mobile_money")]
    public MobileMoneyOperator? RecipientMobileMoneyOperator { get; set; }
}


/// <summary>
/// Represents recipient bank account details included in a payout response or webhook.
/// </summary>
public sealed class BankRecipientAccountDetails
{
    /// <summary>
    /// Gets or sets the recipient bank name.
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the recipient bank identifier.
    /// </summary>
    [JsonPropertyName("bank_uuid")]
    public Guid BankId { get; set; }

    /// <summary>
    /// Gets or sets the recipient account name.
    /// </summary>
    [JsonPropertyName("account_name")]
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets or sets the recipient account number.
    /// </summary>
    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }
}