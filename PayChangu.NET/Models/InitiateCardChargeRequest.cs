using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents a request to initiate a card charge through PayChangu.
/// </summary>
public sealed class InitiateCardChargeRequest
{
    /// <summary>
    /// Gets or sets the card number.
    /// </summary>
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the card expiry date.
    /// </summary>
    [JsonPropertyName("expiry")]
    public string Expiry { get; set; } = null!;

    /// <summary>
    /// Gets or sets the card verification value (CVV).
    /// </summary>
    [JsonPropertyName("cvv")]
    public string Cvv { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cardholder's name.
    /// </summary>
    [JsonPropertyName("cardholder_name")]
    public string CardholderName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction currency.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique charge identifier.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string ChargeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URL to which the customer is redirected
    /// after the card authentication process.
    /// </summary>
    [JsonPropertyName("redirect_url")]
    public string RedirectUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cardholder's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}