using PayChangu.NET.Json.Converters;
using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System;
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
    public MobileMoneyPayoutTransaction Data { get; set; } = null!;
}


/// <summary>
/// Represents the transaction details returned when initiating a mobile money payout.
/// </summary>
public sealed class MobileMoneyPayoutTransaction
{
    /// <summary>
    /// Gets or sets the payout amount.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PayChanguDecimalJsonConverter))]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier supplied for the payout.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PayChangu payout reference identifier.
    /// </summary>
    [JsonPropertyName("ref_id")]
    public string ReferenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider transaction identifier.
    /// </summary>
    [JsonPropertyName("trans_id")]
    public string? TransactionId { get; set; }

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
    /// Gets or sets the payout trace identifier.
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the payout transaction status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recipient's mobile phone number.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of payout attempts.
    /// </summary>
    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the transaction currency.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the environment in which the payout was processed.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time the payout was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time the payout was completed.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the event type associated with the payout.
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mobile money operator details.
    /// </summary>
    [JsonPropertyName("mobile_money")]
    public MobileMoneyOperator MobileMoney { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction charges.
    /// </summary>
    [JsonPropertyName("transaction_charges")]
    public PayChanguTransactionCharges TransactionCharges { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payout authorization details.
    /// </summary>
    [JsonPropertyName("authorization")]
    public PayChanguTransactionAuthorization? Authorization { get; set; }
}