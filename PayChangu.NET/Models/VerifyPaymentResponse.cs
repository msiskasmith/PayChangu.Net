using PayChangu.NET.Json.Converters;
using PayChangu.NET.Models.Base;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the normalized response returned when verifying a PayChangu payment,
/// regardless of the payment flow or payment method used.
/// </summary>
public sealed class VerifyPaymentResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the payment flow used to process the transaction.
    /// </summary>
    public PayChanguPaymentFlow PaymentFlow { get; set; }

    /// <summary>
    /// Gets or sets the payment method used to process the transaction.
    /// </summary>
    public PayChanguPaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets the identifier used to identify the transaction during verification.
    /// This is the merchant transaction reference for Checkout or the charge identifier
    /// for Direct Charge.
    /// </summary>
    public string TransactionIdentifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction reference.
    /// This is the Checkout transaction reference or the Direct Charge reference identifier,
    /// when available.
    /// </summary>
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Gets or sets the PayChangu charge identifier, when available.
    /// </summary>
    public string? ChargeId { get; set; }

    /// <summary>
    /// Gets or sets the transaction reference assigned by PayChangu, when available.
    /// </summary>
    public string? ProviderReference { get; set; }

    /// <summary>
    /// Gets or sets the provider transaction identifier, when available.
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the transaction trace identifier, when available.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the event type associated with the transaction.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets the environment in which the transaction was processed.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the status returned by the PayChangu verification API.
    /// </summary>
    public string ApiStatus { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total number of payment attempts, when available.
    /// </summary>
    public int? Attempts { get; set; }

    /// <summary>
    /// Gets or sets the currency used for the transaction.
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>

    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the charges applied to the transaction, when available.
    /// </summary>
    public PayChanguVerificationCharges? Charges { get; set; }

    /// <summary>
    /// Gets or sets the checkout customization information, when applicable.
    /// </summary>
    public VerificationCustomization? Customization { get; set; }

    /// <summary>
    /// Gets or sets additional metadata associated with the transaction.
    /// </summary>
    public JsonElement? Meta { get; set; }

    /// <summary>
    /// Gets or sets the payment authorization details, when available.
    /// </summary>
    public VerificationAuthorization? Authorization { get; set; }

    /// <summary>
    /// Gets or sets the mobile money operator details, when applicable.
    /// </summary>
    public MobileMoneyOperator? MobileMoney { get; set; }

    /// <summary>
    /// Gets or sets the customer information associated with the transaction.
    /// </summary>
    public VerificationCustomer? Customer { get; set; }

    /// <summary>
    /// Gets or sets the activity logs associated with the transaction.
    /// </summary>
    public List<VerificationLog> Logs { get; set; } =
        new List<VerificationLog>();

    /// <summary>
    /// Gets or sets the date and time when the transaction was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Represents the charges applied to a verified PayChangu transaction.
/// </summary>
public sealed class PayChanguVerificationCharges
{
    /// <summary>
    /// Gets or sets the currency in which the transaction charges are expressed.
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction charge amount.
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Represents the normalized authorization details of a PayChangu transaction.
/// </summary>
public sealed class VerificationAuthorization
{
    /// <summary>
    /// Gets or sets the payment channel used to process the transaction.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Gets or sets the masked card number, when applicable.
    /// </summary>
    public string? CardNumber { get; set; }

    /// <summary>
    /// Gets or sets the card expiry date, when applicable.
    /// </summary>
    public string? Expiry { get; set; }

    /// <summary>
    /// Gets or sets the card brand, when applicable.
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Gets or sets the payment provider, when available.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the mobile number used to authorize the payment, when available.
    /// </summary>
    public string? MobileNumber { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank identifier, when applicable.
    /// </summary>
    public Guid PayerBankId { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank name, when applicable.
    /// </summary>
    public string? PayerBank { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank account number, when applicable.
    /// </summary>
    public string? PayerAccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the payer's bank account name, when applicable.
    /// </summary>
    public string? PayerAccountName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payment was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}


/// <summary>
/// Represents the customer associated with a verified PayChangu transaction.
/// </summary>
public sealed class VerificationCustomer
{
    /// <summary>
    /// Gets or sets the customer's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the customer's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the customer's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the customer's mobile number, when available.
    /// </summary>
    public string? Mobile { get; set; }
}

/// <summary>
/// Represents the checkout customization information associated with a transaction.
/// </summary>
public sealed class VerificationCustomization
{
    /// <summary>
    /// Gets or sets the title displayed during checkout.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description displayed during checkout.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL of the logo displayed during checkout.
    /// </summary>
    public string? Logo { get; set; }
}

/// <summary>
/// Represents an activity log entry associated with a verified transaction.
/// </summary>
public sealed class VerificationLog
{
    /// <summary>
    /// Gets or sets the type of activity recorded in the log entry.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the message describing the recorded activity.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the log entry was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }
}