using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents a request to initiate a mobile money payout.
/// </summary>
public sealed class InitiateMobileMoneyPayoutRequest
{
    /// <summary>
    /// Gets or sets the recipient's mobile phone number.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the mobile money operator.
    /// Obtain this identifier from PayChangu's supported operators endpoint.
    /// </summary>
    [JsonPropertyName("mobile_money_operator_ref_id")]
    public string MobileMoneyOperatorRefId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the amount to be sent.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this payout.
    /// This value must be unique for every transaction.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

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

    /// <summary>
    /// Gets or sets the simulated transaction status for sandbox testing only.
    /// </summary>
    /// <remarks>
    /// This property is intended exclusively for testing in PayChangu's sandbox
    /// environment. It allows developers to simulate payout outcomes without
    /// processing actual transactions.
    ///
    /// Accepted values are "failed" and "successful".
    /// Do not set this property when using the production environment.
    /// </remarks>
    [JsonPropertyName("transaction_status")]
    public string? TransactionStatus { get; set; }
}