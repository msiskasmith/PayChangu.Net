
using PayChangu.NET.Json.Converters;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents a webhook event payload received from PayChangu.
/// </summary>
public sealed class PayChanguWebhookEvent
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction currency.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PayChanguDecimalJsonConverter))]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction charge.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName("charge")]
    public decimal Charge { get; set; }

    /// <summary>
    /// Gets or sets the transaction mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PayChangu charge identifier, when available.
    /// </summary>
    [JsonPropertyName("charge_id")]
    public string? ChargeId { private get; set; }

    /// <summary>
    /// Gets or sets the PayChangu transaction reference.
    /// </summary>
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = null!;

    /// <summary>
    /// Gets or sets the merchant transaction reference.
    /// </summary>
    [JsonPropertyName("tx_ref")]
    public string? TransactionReference { private get; set; }

    /// <summary>
    /// Gets the transaction identifier to use when verifying the payment.
    /// Returns the merchant transaction reference for Standard Checkout
    /// or the PayChangu charge identifier for Direct Charge.
    /// </summary>
    [JsonIgnore]
    public string TransactionIdentifier =>
    PaymentFlow switch
    {
        PayChanguPaymentFlow.Checkout =>
            TransactionReference ?? throw new JsonException(
                "The Checkout webhook is missing tx_ref."),

        PayChanguPaymentFlow.DirectCharge or PayChanguPaymentFlow.Payout =>
            ChargeId ?? throw new JsonException(
                "The webhook is missing charge_id."),

        _ => throw new JsonException(
            "The webhook payment flow could not be determined.")
    };

    /// <summary>
    /// Gets or sets the payee's first name, if provided.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? PayeeFirstName { get; set; }

    /// <summary>
    /// Gets or sets the payee's last name, if provided.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? PayeeLastName { get; set; }

    /// <summary>
    /// Gets or sets the payee's email address, if provided.
    /// </summary>
    [JsonPropertyName("email")]
    public string? PayeeEmail { get; set; }

    /// <summary>
    /// Gets or sets the customer details.
    /// </summary>
    [JsonPropertyName("customer")]
    public PayChanguWebhookCustomer? Customer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction authorization details.
    /// </summary>
    [JsonPropertyName("authorization")]
    public PayChanguTransactionSource? Source { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recipient bank account details for a payout.
    /// </summary>
    [JsonPropertyName("recipient_account_details")]
    public BankRecipientAccountDetails? RecipientAccountDetails { get; set; }

    /// <summary>
    /// Gets or sets the mobile money operator details, if applicable.
    /// </summary>
    [JsonPropertyName("mobile_money")]
    public MobileMoneyOperator? RecipientMobileMoneyOperator { get; set; }

    /// <summary>
    /// Gets or sets the transaction creation date and time.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the transaction update date and time.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets the payment flow associated with this webhook event.
    /// </summary>
    [JsonIgnore]
    public PayChanguPaymentFlow PaymentFlow =>
    EventType switch
    {
        "checkout.payment" => PayChanguPaymentFlow.Checkout,
        "api.charge.payment" => PayChanguPaymentFlow.DirectCharge,
        "api.payout" => PayChanguPaymentFlow.Payout,
        _ => PayChanguPaymentFlow.Unknown
    };

    /// <summary>
    /// Gets the payment method associated with this webhook event.
    /// </summary>
    [JsonIgnore]
    public PayChanguPaymentMethod PaymentMethod
    {
        get
        {
            if (PaymentFlow == PayChanguPaymentFlow.Payout)
            {
                if (RecipientAccountDetails != null)
                    return PayChanguPaymentMethod.BankTransfer;

                if (RecipientMobileMoneyOperator != null)
                    return PayChanguPaymentMethod.MobileMoney;

                return PayChanguPaymentMethod.Unknown;
            }

            if (Source == null)
                return PayChanguPaymentMethod.Unknown;

            if (Source.MobileMoney != null)
                return PayChanguPaymentMethod.MobileMoney;

            if (Source.BankPayment != null)
                return PayChanguPaymentMethod.BankTransfer;

            if (Source.Card != null)
                return PayChanguPaymentMethod.Card;

            return Source.Channel switch
            {
                "Mobile Money" => PayChanguPaymentMethod.MobileMoney,
                "Mobile Bank Transfer" => PayChanguPaymentMethod.BankTransfer,
                "Card" => PayChanguPaymentMethod.Card,
                _ => PayChanguPaymentMethod.Unknown
            };
        }
    }
}

/// <summary>
/// Represents the authorization details of a PayChangu transaction.
/// </summary>
public sealed class PayChanguTransactionSource
{
    /// <summary>
    /// Gets or sets the payment channel.
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the card payment details, when applicable.
    /// </summary>
    [JsonPropertyName("card_details")]
    public PayChanguCardDetails? Card { get; set; }

    /// <summary>
    /// Gets or sets the bank payment details, when applicable.
    /// </summary>
    [JsonPropertyName("bank_payment_details")]
    public PayChanguBankPaymentDetails? BankPayment { get; set; }

    /// <summary>
    /// Gets or sets the mobile money payment details, when applicable.
    /// </summary>
    [JsonPropertyName("mobile_money")]
    public PayChanguMobileMoneyDetails? MobileMoney { get; set; }

    /// <summary>
    /// Gets or sets the payment completion date and time, when available.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Represents mobile money payment details.
/// </summary>
public sealed class PayChanguMobileMoneyDetails
{
    /// <summary>
    /// Gets or sets the customer's mobile number.
    /// </summary>
    [JsonPropertyName("mobile_number")]
    public string MobileNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mobile money operator.
    /// </summary>
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mobile money transaction identifier.
    /// </summary>
    [JsonPropertyName("trans_id")]
    public string TransId { get; set; } = null!;
}

/// <summary>
/// Represents card payment details.
/// </summary>
public sealed class PayChanguCardDetails
{
    /// <summary>
    /// Gets or sets the card number returned by PayChangu.
    /// </summary>
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the card brand.
    /// </summary>
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = null!;
}

/// <summary>
/// Represents bank payment details.
/// </summary>
public sealed class PayChanguBankPaymentDetails
{
    /// <summary>
    /// Gets or sets the payer bank identifier.
    /// </summary>
    [JsonPropertyName("payer_bank_uuid")]
    public Guid PayerBankId { get; set; }

    /// <summary>
    /// Gets or sets the payer bank name.
    /// </summary>
    [JsonPropertyName("payer_bank")]
    public string PayerBank { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payer bank account number.
    /// </summary>
    [JsonPropertyName("payer_account_number")]
    public string PayerAccountNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payer bank account name.
    /// </summary>
    [JsonPropertyName("payer_account_name")]
    public string PayerAccountName { get; set; } = null!;
}

/// <summary>
/// Represents customer information included in a webhook event.
/// </summary>
public sealed class PayChanguWebhookCustomer
{
    /// <summary>
    /// Gets or sets the PayChangu customer reference.
    /// </summary>
    [JsonPropertyName("customer_ref")]
    public string CustomerRef { get; set; } = null!;

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
    /// Gets or sets the customer's phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the merchant reference identifier.
    /// </summary>
    [JsonPropertyName("merchant_reference_identifier")]
    public string? MerchantReferenceIdentifier { get; set; }
}

/// <summary>
/// Represents the payment flow used to process a PayChangu transaction.
/// </summary>
public enum PayChanguPaymentFlow
{
    /// <summary>
    /// The payment flow could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// The payment was initiated through PayChangu Standard Checkout.
    /// </summary>
    Checkout,

    /// <summary>
    /// The payment was initiated directly through a PayChangu payment endpoint.
    /// </summary>
    DirectCharge,

    /// <summary>
    /// The transaction was initiated through a PayChangu payout endpoint.
    /// </summary>
    Payout
}

/// <summary>
/// Represents the payment method used for a PayChangu transaction.
/// </summary>
public enum PayChanguPaymentMethod
{
    /// <summary>
    /// The payment method could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// The payment was made using a mobile money service.
    /// </summary>
    MobileMoney,

    /// <summary>
    /// The payment was made using a bank transfer.
    /// </summary>
    BankTransfer,

    /// <summary>
    /// The payment was made using a payment card.
    /// </summary>
    Card
}
