# PayChangu.NET

A .NET SDK for integrating with the [PayChangu](https://paychangu.com/) payment gateway.

PayChangu.NET provides a strongly typed, asynchronous interface for interacting with PayChangu APIs from .NET applications. It simplifies payment initiation, transaction verification, direct charges, and payouts through a reusable client with built-in dependency injection support.

## Features

- Standard Checkout payment initiation.
- Mobile Money Direct Charge initiation and transaction details retrieval.
- Bank Transfer Direct Charge initiation and transaction details retrieval.
- Card Direct Charge initiation, verification, and refunds.
- Payment verification across supported payment flows.
- Mobile Money payout initiation.
- Bank payout initiation and transaction details retrieval.
- Retrieval of supported Mobile Money operators.
- Retrieval of supported banks and payout providers.
- Wallet balance retrieval.
- Paginated bank payout transaction retrieval.
- Strongly typed request and response models.
- Dependency injection support through `IHttpClientFactory`.
- Configurable API base URL.
- Bearer token authentication.
- Asynchronous operations with cancellation token support.
- Standardized API response handling.
- Webhook signature validation and event parsing.

## Installation

Once the package is published, install it using the .NET CLI:

```bash
dotnet add package PayChangu.NET
```

## Configuration

Register PayChangu.NET with your application's dependency injection container using `AddPayChangu`.

The PayChangu secret key is required. The API base URL defaults to `https://api.paychangu.com/`.

```csharp
builder.Services.AddPayChangu(options =>
{
    options.SecretKey = builder.Configuration["PayChangu:SecretKey"]!;
});
```

If you intend to process webhooks, configure the webhook secret as well:

```csharp
builder.Services.AddPayChangu(options =>
{
    options.SecretKey = builder.Configuration["PayChangu:SecretKey"]!;
    options.WebhookSecret = builder.Configuration["PayChangu:WebhookSecret"];
});
```

To override the default API base URL:

```csharp
builder.Services.AddPayChangu(options =>
{
    options.SecretKey = builder.Configuration["PayChangu:SecretKey"]!;
    options.BaseUrl = "https://api.paychangu.com/";
});
```

### HTTP client configuration

`AddPayChangu` accepts an optional `configureHttpClient` action. Use it when you need to customize the underlying `HttpClient` configuration.

```csharp
builder.Services.AddPayChangu(
    options =>
    {
        options.SecretKey = builder.Configuration["PayChangu:SecretKey"]!;
    },
    httpClient =>
    {
        // Add any required HttpClient configuration here.
    });
```

The SDK automatically configures Bearer token authentication and the JSON `Accept` header.

### Dependency injection

Registration provides the following services:

- `IPayChanguClient` — communicates with the PayChangu API.
- `IPayChanguWebhookService` — validates webhook signatures and parses webhook events. A webhook secret must be configured when using this service.

Keep your secret keys in configuration or a secure secrets provider. Do not hardcode them in source code or expose them in client-side applications.

## Standard Checkout

Standard Checkout allows you to initiate a payment and redirect the customer to a PayChangu-hosted checkout page.

### Initiating a payment

Inject `IPayChanguClient` into your application service or controller. Create an `InitiateCheckoutRequest` and pass it to `InitiatePaymentAsync`.

```csharp
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PayChangu.NET;
using PayChangu.NET.Models;

public class PaymentsController : Controller
{
    private readonly IPayChanguClient _payChanguClient;

    public PaymentsController(IPayChanguClient payChanguClient)
    {
        _payChanguClient = payChanguClient;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCheckout(
        CancellationToken cancellationToken)
    {
        var request = new InitiateCheckoutRequest
        {
            Amount = 5000m,
            Currency = "MWK",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Banda",
            TransactionReference = Guid.NewGuid().ToString(),

            CallbackUrl = "https://example.com/payments/callback",
            ReturnUrl = "https://example.com/payment/return",

            Customization = new PaymentCustomization
            {
                Title = "Order Payment",
                Description = "Payment for order #12345"
            },

            Meta = new PaymentMeta
            {
                Id = Guid.NewGuid(),
                Response = "order-12345"
            }
        };

        var response = await _payChanguClient.InitiatePaymentAsync(
            request,
            cancellationToken);

        if (!response.Success || response.Result?.Data == null)
        {
            // Handle the unsuccessful API response.
            // Inspect response.ErrorMessage and response.StatusCode.
            return StatusCode(
                (int)(response.StatusCode ?? HttpStatusCode.BadGateway));
        }

        // Redirect the customer to PayChangu's hosted checkout page.
        return Redirect(response.Result.Data.CheckoutUrl);
    }
}
```

A successful initiation response provides the checkout URL through `response.Result.Data.CheckoutUrl`. Redirect the customer to this URL to complete the payment.

### Callback and return URLs

Standard Checkout uses two separate URLs for different purposes:

- **`CallbackUrl`** — PayChangu sends a callback for successful transactions to this URL. Provide a publicly accessible endpoint in your application to handle it.
- **`ReturnUrl`** — PayChangu redirects the customer to this URL when checkout is canceled or fails. Use it to bring the customer back to your application and display the appropriate outcome.

These URLs serve different purposes from the webhook endpoint. Configure your webhook endpoint separately in the PayChangu dashboard.

**Important:** Do not rely solely on a callback or customer redirect to fulfil an order. Verify the transaction and confirm its final status before treating the payment as completed.

## Mobile Money Direct Charge

PayChangu.NET supports initiating Mobile Money Direct Charge payments. This sends a payment request to the customer's mobile phone, prompting them to authorize the transaction.

### Initiating a Mobile Money Direct Charge

First, retrieve the supported mobile money operators using `GetMobileMoneyOperatorsAsync`. Each operator provides a `RefId` that must be supplied when initiating a charge.

```csharp
var operatorsResponse = await _payChanguClient.GetMobileMoneyOperatorsAsync(
    cancellationToken);

if (!operatorsResponse.Success || operatorsResponse.Result == null)
{
    // Handle the unsuccessful API response.
    return;
}

// Select the appropriate operator.
var operatorRefId = operatorsResponse.Result.Data
    .First(x => x.Name == "Airtel Money")
    .RefId;
```

Once you have the operator reference ID, create and submit the charge request:

```csharp
var request = new InitiateMobileMoneyDirectChargeRequest
{
    Mobile = "+265991234567",
    MobileMoneyOperatorRefId = operatorRefId,
    Amount = 5000m,
    ChargeId = Guid.NewGuid().ToString(),
    Email = "customer@example.com",
    FirstName = "John",
    LastName = "Banda"
};

var response = await _payChanguClient.InitiateMobileMoneyDirectChargeAsync(
    request,
    cancellationToken);

if (!response.Success || response.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect response.ErrorMessage and response.StatusCode.
    return;
}

// The charge request was accepted by PayChangu.
var chargeId = response.Result.Data.ChargeId;
var status = response.Result.Data.Status;
```

### Verifying the transaction

A successful initiation response does not mean the customer has completed the payment. The customer must authorize the payment on their mobile device.

Use `VerifyPaymentAsync` with the returned charge identifier to check the transaction status:

```csharp
var verificationResponse = await _payChanguClient.VerifyPaymentAsync(
    chargeId,
    PayChanguPaymentFlow.DirectCharge,
    PayChanguPaymentMethod.MobileMoney,
    cancellationToken);

if (!verificationResponse.Success ||
    verificationResponse.Result == null)
{
    // Handle the unsuccessful verification request.
    return;
}

// Check the actual transaction status before fulfilling the order.
var paymentStatus = verificationResponse.Result.Status;
```

The verification response provides normalized transaction details, including the payment status, amount, currency, customer information, and transaction timestamps.

**Important:** Always verify the transaction before treating a Mobile Money Direct Charge as successfully paid. Do not rely solely on the initiation response.

## Bank Transfer Direct Charge

PayChangu.NET supports Bank Transfer Direct Charge payments. This flow generates bank account details that the customer can use to complete a payment.

### Initiating a Bank Transfer Direct Charge

Create an \`InitiateBankTransferDirectChargeRequest\` and pass it to \`InitiateBankTransferDirectChargeAsync\`.

\`\`\`csharp
using System;

var chargeId = Guid.NewGuid().ToString();

var request = new InitiateBankTransferDirectChargeRequest
{
    Amount = 5000m,
    Currency = "MWK",
    ChargeId = chargeId,
    Email = "customer@example.com",
    FirstName = "John",
    LastName = "Banda",
    Mobile = "+265991234567",
    CreatePermanentAccount = false
};

var response = await _payChanguClient.InitiateBankTransferDirectChargeAsync(
    request,
    cancellationToken);

if (!response.Success || response.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect response.ErrorMessage and response.StatusCode.
    return;
}

var paymentDetails = response.Result.Data.PaymentAccountDetails;
var transaction = response.Result.Data.Transaction;

// Provide these bank details to the customer.
var bankName = paymentDetails.BankName;
var accountNumber = paymentDetails.AccountNumber;
var accountName = paymentDetails.AccountName;
var accountExpiry = DateTimeOffset.FromUnixTimeSeconds(
    paymentDetails.AccountExpirationTimestamp);

var transactionStatus = transaction.Status;
\`\`\`

The response provides the bank name, account number, account name, account expiration timestamp, and transaction details.

The SDK sets the payment method to \`mobile_bank_transfer\`; you do not need to set it yourself. The currency defaults to \`MWK\`. \`CreatePermanentAccount\` is an optional nullable flag and defaults to \`null\` when omitted.

### Retrieving Bank Transfer Direct Charge details

Use \`GetSingleBankDirectChargeDetailsAsync\` with the charge identifier to retrieve the transaction details:

\`\`\`csharp
var detailsResponse = await _payChanguClient.GetSingleBankDirectChargeDetailsAsync(
    chargeId,
    cancellationToken);

if (!detailsResponse.Success || detailsResponse.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    return;
}

var transactionDetails = detailsResponse.Result.Data.Transaction;
var status = transactionDetails.Status;
\`\`\`

### Verifying the transaction

Initiating a Bank Transfer Direct Charge generates the bank account details; it does not mean the customer has completed the payment.

Use \`VerifyPaymentAsync\` to verify the transaction:

\`\`\`csharp
var verificationResponse = await _payChanguClient.VerifyPaymentAsync(
    chargeId,
    PayChanguPaymentFlow.DirectCharge,
    PayChanguPaymentMethod.BankTransfer,
    cancellationToken);

if (!verificationResponse.Success ||
    verificationResponse.Result == null)
{
    // Handle the unsuccessful verification request.
    return;
}

var paymentStatus = verificationResponse.Result.Status;
\`\`\`

**Important:** Always verify the transaction and confirm its final status before fulfilling an order. Do not treat the initiation response or the generated bank account details as proof of payment.

## Webhooks

PayChangu uses a single webhook endpoint for notifications from Standard Checkout, Direct Charge, and Payout transactions.

Configure your application's publicly accessible webhook endpoint in the PayChangu dashboard. This endpoint is separate from the `CallbackUrl` and `ReturnUrl` supplied when initiating a Standard Checkout payment.

PayChangu.NET provides `IPayChanguWebhookService` to validate webhook signatures and deserialize incoming notifications into a strongly typed `PayChanguWebhookEvent`.

### Validating and parsing a webhook

Always validate the signature against the original, unmodified request body before processing the event.

```csharp
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PayChangu.NET.Models;

public class PayChanguWebhookController : ControllerBase
{
    private readonly IPayChanguWebhookService _webhookService;

    public PayChanguWebhookController(
        IPayChanguWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        var signature = Request.Headers["Signature"].ToString();

        if (!_webhookService.VerifySignature(payload, signature))
        {
            return Unauthorized();
        }

        PayChanguWebhookEvent webhookEvent;

        try
        {
            webhookEvent = _webhookService.ParseWebhook(payload);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        // Process the webhook event here.

        return Ok();
    }
}
```

The example uses ASP.NET Core controller conventions. Add the appropriate ASP.NET Core namespaces to your controller.

### Handling webhook events

The `PayChanguWebhookEvent` model exposes the event type, transaction details, payment flow, payment method, and relevant customer or recipient information.

The SDK recognizes these event types:

| Event type | Payment flow | Transaction identifier |
|---|---|---|
| `checkout.payment` | `Checkout` | Merchant transaction reference (`tx_ref`) |
| `api.charge.payment` | `DirectCharge` | PayChangu charge identifier (`charge_id`) |
| `api.payout` | `Payout` | PayChangu charge identifier (`charge_id`) |

You can use the derived `PaymentFlow`, `PaymentMethod`, and `TransactionIdentifier` properties to determine how to handle an event.

```csharp
switch (webhookEvent.PaymentFlow)
{
    case PayChanguPaymentFlow.Checkout:
    case PayChanguPaymentFlow.DirectCharge:
        var transactionIdentifier = webhookEvent.TransactionIdentifier;
        var paymentMethod = webhookEvent.PaymentMethod;

        // Handle or verify the payment.
        break;

    case PayChanguPaymentFlow.Payout:
        // Handle the payout notification.
        break;

    default:
        // Handle an unrecognized event type.
        break;
}
```

`TransactionIdentifier` returns the appropriate identifier for Checkout and Direct Charge events. Although it also exposes the charge identifier for Payout events, payout notifications should be handled through your payout workflow rather than passed to `VerifyPaymentAsync`.

**Important:** Signature validation confirms that the webhook payload was signed using the configured webhook secret. Your application should still process transaction statuses appropriately, verify payments where applicable, and avoid treating an initiation or pending notification as a completed transaction.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
