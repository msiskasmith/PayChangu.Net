using PayChangu.NET.Json.Converters;
using PayChangu.NET.Models.Base;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned by PayChangu after initiating a payment.
/// </summary>
public sealed class InitiateCheckoutResponse : BasePaychanguResponse
{

    /// <summary>
    /// Gets or sets the payment response data.
    /// </summary>
    [JsonPropertyName("data")]
    public PaymentResponseData Data { get; set; } = null!;
}

/// <summary>
/// Contains the details of the payment checkout session created by PayChangu.
/// </summary>
public sealed class PaymentResponseData
{
    /// <summary>
    /// Gets or sets the event associated with the response.
    /// </summary>
    /// <example>checkout.session:created</example>
    [JsonPropertyName("event")]
    public string Event { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URL where the customer can complete the payment.
    /// </summary>
    /// <remarks>
    /// Redirect the customer to this URL to proceed with the checkout process.
    /// </remarks>
    [JsonPropertyName("checkout_url")]
    public string CheckoutUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the details of the created checkout session.
    /// </summary>
    [JsonPropertyName("data")]
    public CheckoutSessionData Session { get; set; } = null!;
}

/// <summary>
/// Represents the details of a PayChangu checkout session.
/// </summary>
public sealed class CheckoutSessionData
{
    /// <summary>
    /// Gets or sets the transaction reference supplied when initiating the payment.
    /// </summary>
    [JsonPropertyName("tx_ref")]
    public string TransactionReference { get; set; } = null!;

    /// <summary>
    /// Gets or sets the currency of the transaction.
    /// </summary>
    /// <example>MWK</example>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PayChanguDecimalJsonConverter))]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the payment mode.
    /// </summary>
    /// <remarks>
    /// Indicates the mode in which the checkout session was created,
    /// such as test or live.
    /// </remarks>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current status of the checkout session.
    /// </summary>
    /// <remarks>
    /// This represents the session status returned during payment initiation.
    /// It should not be treated as final confirmation that the customer
    /// has completed the payment.
    /// </remarks>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;
}