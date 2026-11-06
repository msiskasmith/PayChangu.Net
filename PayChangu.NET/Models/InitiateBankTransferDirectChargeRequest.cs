using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents a request to initiate a Bank Transfer Direct Charge payment.
/// </summary>
public sealed class InitiateBankTransferDirectChargeRequest
{
    /// <summary>
    /// Gets or sets the amount to be paid.
    /// </summary>
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = null!;

    /// <summary>
    /// Gets or sets the currency used for the payment.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "MWK";

    /// <summary>
    /// Gets or sets the payment method.
    /// </summary>
    /// <summary>
    /// Gets the payment method.
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; private set; } = "mobile_bank_transfer";

    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
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

    /// <summary>
    /// Gets or sets the customer's mobile number.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// Gets or sets whether to create a permanent bank account.
    /// </summary>
    [JsonPropertyName("create_permanent_account")]
    public bool? CreatePermanentAccount { get; set; }
}