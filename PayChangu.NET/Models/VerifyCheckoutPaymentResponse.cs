
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when verifying a PayChangu transaction.
/// </summary>
internal sealed class VerifyCheckoutPaymentResponse
{
    /// <summary>
    /// Gets or sets the status of the API response.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the message returned by the PayChangu API.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the verified transaction details.
    /// </summary>
    [JsonPropertyName("data")]
    public CheckoutPaymentVerificationData Data { get; set; } = null!;
}

/// <summary>
/// Contains the details of a verified PayChangu transaction.
/// </summary>
internal sealed class CheckoutPaymentVerificationData
{
    /// <summary>
    /// Gets or sets the event type associated with the transaction.
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the merchant-generated transaction reference.
    /// </summary>
    [JsonPropertyName("tx_ref")]
    public string TransactionReference { get; set; } = null!;

    /// <summary>
    /// Gets or sets the environment in which the transaction was processed,
    /// such as live or sandbox.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of transaction.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the final status of the transaction.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total number of payment attempts made for the transaction.
    /// </summary>
    [JsonPropertyName("number_of_attempts")]
    public int NumberOfAttempts { get; set; }

    /// <summary>
    /// Gets or sets the unique transaction reference assigned by PayChangu.
    /// </summary>
    [JsonPropertyName("reference")]
    public string ProviderReference { get; set; } = null!;

    /// <summary>
    /// Gets or sets the currency used for the transaction.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the charges applied to the transaction.
    /// </summary>
    [JsonPropertyName("charges")]
    public decimal Charges { get; set; }

    /// <summary>
    /// Gets or sets the checkout customization information associated
    /// with the transaction.
    /// </summary>
    [JsonPropertyName("customization")]
    public CheckoutPaymentVerificationCustomization? Customization { get; set; }

    /// <summary>
    /// Gets or sets additional metadata associated with the transaction.
    /// The structure of this data depends on the metadata supplied
    /// when the payment was initiated.
    /// </summary>
    [JsonPropertyName("meta")]
    public JsonElement? Meta { get; set; }

    /// <summary>
    /// Gets or sets the payment authorization details.
    /// </summary>
    [JsonPropertyName("authorization")]
    public CheckoutPaymentAuthorization? Authorization { get; set; }

    /// <summary>
    /// Gets or sets the customer information associated with the transaction.
    /// </summary>
    [JsonPropertyName("customer")]
    public CheckoutPaymentCustomer? Customer { get; set; }

    /// <summary>
    /// Gets or sets the activity logs associated with the transaction.
    /// </summary>
    [JsonPropertyName("logs")]
    public List<CheckoutPaymentLog> Logs { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the transaction was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Represents the checkout customization information associated
/// with a transaction.
/// </summary>
internal sealed class CheckoutPaymentVerificationCustomization
{
    /// <summary>
    /// Gets or sets the title displayed during checkout.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description displayed during checkout.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL of the logo displayed during checkout.
    /// </summary>
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}

/// <summary>
/// Represents the authorization details of a payment transaction.
/// </summary>
internal sealed class CheckoutPaymentAuthorization
{
    /// <summary>
    /// Gets or sets the payment channel used to process the transaction.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary>
    /// Gets or sets the masked card number associated with the payment,
    /// when applicable.
    /// </summary>
    [JsonPropertyName("card_number")]
    public string? MaskedCardNumber { get; set; }

    /// <summary>
    /// Gets or sets the card expiry date, when applicable.
    /// </summary>
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    /// <summary>
    /// Gets or sets the brand of the payment card, when applicable.
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// Gets or sets the payment provider used to process the transaction.
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the mobile number used to authorize the payment,
    /// when applicable.
    /// </summary>
    [JsonPropertyName("mobile_number")]
    public string? MobileNumber { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payment was completed.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Represents the customer associated with a payment transaction.
/// </summary>
internal sealed class CheckoutPaymentCustomer
{
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

/// <summary>
/// Represents an activity log entry associated with a transaction.
/// </summary>
internal sealed class CheckoutPaymentLog
{
    /// <summary>
    /// Gets or sets the type of activity recorded in the log entry.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the message describing the recorded activity.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the log entry was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}
