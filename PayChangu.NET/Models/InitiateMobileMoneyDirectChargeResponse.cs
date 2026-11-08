using PayChangu.NET.Json.Converters;
using System;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when initiating a direct mobile money charge.
/// </summary>
public sealed class InitiateMobileMoneyDirectChargeResponse
{
    /// <summary>
    /// Gets or sets the API response status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the initiated payment details.
    /// </summary>
    [JsonPropertyName("data")]
    public MobileMoneyChargeData Data { get; set; } = null!;
}

/// <summary>
/// Represents the details of an initiated mobile money charge.
/// </summary>
public sealed class MobileMoneyChargeData
{
    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PayChanguDecimalJsonConverter))]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the PayChangu charge identifier.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the merchant transaction reference.
    /// </summary>
    [JsonPropertyName("ref_id")]
    public string ReferenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PayChangu transaction identifier.
    /// </summary>
    [JsonPropertyName("trans_id")]
    public string TransactionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer's first name.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer's last name.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer's mobile number.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of payment attempts.
    /// </summary>
    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the payment currency.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction environment mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time the charge was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time the charge was completed.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the mobile money operator information.
    /// </summary>
    [JsonPropertyName("mobile_money")]
    public MobileMoneyOperator MobileMoney { get; set; } = null!;
}