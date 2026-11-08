using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the transaction details returned when verifying a PayChangu
/// Direct Charge payment, regardless of the payment method used.
/// </summary>
public sealed class PayChanguTransaction
{
    /// <summary>
    /// Gets or sets the PayChangu charge identifier.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PayChangu transaction reference.
    /// </summary>
    [JsonPropertyName("ref_id")]
    public string ReferenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider transaction identifier, when available.
    /// </summary>
    [JsonPropertyName("trans_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the transaction currency.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the customer's first name, when available.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the customer's last name, when available.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the customer's email address, when available.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the payment type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the transaction trace identifier, when available.
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer's mobile number, when available.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// Gets or sets the number of payment attempts.
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
    /// Gets or sets the transaction completion date and time, when available.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the webhook event type associated with the transaction.
    /// </summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets the mobile money operator details, when applicable.
    /// </summary>
    [JsonPropertyName("mobile_money")]
    public MobileMoneyOperator? MobileMoney { get; set; }

    /// <summary>
    /// Gets or sets the transaction charges, when available.
    /// </summary>
    [JsonPropertyName("transaction_charges")]
    public PayChanguTransactionCharges? TransactionCharges { get; set; }

    /// <summary>
    /// Gets or sets the transaction authorization details, when available.
    /// </summary>
    [JsonPropertyName("authorization")]
    public PayChanguTransactionAuthorization? Authorization { get; set; }

    /// <summary>
    /// Gets or sets the transaction processing logs.
    /// </summary>
    [JsonPropertyName("logs")]
    public List<PayChanguTransactionLog> Logs { get; set; } =
        new List<PayChanguTransactionLog>();
}

/// <summary>
/// Represents the charges applied to a PayChangu transaction.
/// </summary>
public sealed class PayChanguTransactionCharges
{
    /// <summary>
    /// Gets or sets the currency in which the transaction charges are expressed.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction charge amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = null!;
}

/// <summary>
/// Represents the authorization details of a PayChangu transaction,
/// including payment-method-specific details.
/// </summary>
public sealed class PayChanguTransactionAuthorization
{
    /// <summary>
    /// Gets or sets the payment channel.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary>
    /// Gets or sets the card number, when applicable.
    /// </summary>
    [JsonPropertyName("card_number")]
    public string? CardNumber { get; set; }

    /// <summary>
    /// Gets or sets the card expiry date, when applicable.
    /// </summary>
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    /// <summary>
    /// Gets or sets the card brand, when applicable.
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// Gets or sets the payment provider, when available.
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the mobile number used for payment, when available.
    /// </summary>
    [JsonPropertyName("mobile_number")]
    public string? MobileNumber { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank identifier, when applicable.
    /// </summary>
    [JsonPropertyName("payer_bank_uuid")]
    public string? PayerBankUuid { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank name, when applicable.
    /// </summary>
    [JsonPropertyName("payer_bank")]
    public string? PayerBank { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank account number, when applicable.
    /// </summary>
    [JsonPropertyName("payer_account_number")]
    public string? PayerAccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank account name, when applicable.
    /// </summary>
    [JsonPropertyName("payer_account_name")]
    public string? PayerAccountName { get; set; }

    /// <summary>
    /// Gets or sets the payment completion date and time, when available.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Represents a processing log entry for a PayChangu transaction.
/// </summary>
public sealed class PayChanguTransactionLog
{
    /// <summary>
    /// Gets or sets the log entry type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the log entry message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time the log entry was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Represents the mobile money operator associated with a PayChangu transaction.
/// </summary>
public sealed class MobileMoneyOperator
{
    /// <summary>
    /// Gets or sets the mobile money operator name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mobile money operator reference identifier.
    /// </summary>
    [JsonPropertyName("ref_id")]
    public string ReferenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the country where the mobile money operator operates.
    /// </summary>
    [JsonPropertyName("country")]
    public string Country { get; set; } = null!;
}
