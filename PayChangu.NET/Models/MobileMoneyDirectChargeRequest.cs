using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents a request to initiate a direct mobile money payment.
/// </summary>
public sealed class MobileMoneyDirectChargeRequest
{
    /// <summary>
    /// Gets or sets the customer's mobile phone number.
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
    /// Gets or sets the amount to be charged.
    /// </summary>
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier for this transaction.
    /// This value must be unique for every charge.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the customer's first name.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the customer's last name.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
}