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
    !verificationResponse.Result.IsValid(chargeId, "MWK", 5000m))
{
    // The verification request failed or the payment details did not match.
    return;
}

// The transaction status, identifier, currency, and amount are valid.
```

The verification response provides normalized transaction details, including the payment status, amount, currency, customer information, and transaction timestamps.

Use the `IsValid` extension method on `VerifyPaymentResponse` to validate the transaction status and ensure the transaction identifier, currency, and amount match your expected values. It returns `false` when the response is null or any of these checks fail.

**Important:** Always verify the transaction before treating a Mobile Money Direct Charge as successfully paid. Do not rely solely on the initiation response.

### Retrieving Mobile Money Direct Charge details

After initiating a Mobile Money Direct Charge, retrieve its transaction details using `GetSingleMobileMoneyChargeDetailsAsync` and the charge identifier:

```csharp
var detailsResponse = await _payChanguClient.GetSingleMobileMoneyChargeDetailsAsync(
    chargeId,
    cancellationToken);

if (!detailsResponse.Success || detailsResponse.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect detailsResponse.ErrorMessage and detailsResponse.StatusCode.
    return;
}

var transaction = detailsResponse.Result.Data;

// Access the transaction details.
var transactionStatus = transaction.Status;
var amount = transaction.Amount;
var currency = transaction.Currency;
var transactionReference = transaction.ReferenceId;
var providerTransactionId = transaction.TransactionId;

// Access the mobile money operator details.
var operatorName = transaction.MobileMoney?.Name;
var operatorCountry = transaction.MobileMoney?.Country;

// Access transaction timestamps.
var createdAt = transaction.CreatedAt;
var completedAt = transaction.CompletedAt;
```

The returned `PayChanguTransaction` provides information such as the charge identifier, transaction reference, amount, currency, status, customer details, mobile money operator, transaction charges, authorization details, processing logs, and timestamps.

**Important:** Retrieving transaction details does not replace payment verification. When deciding whether to fulfil an order, use `VerifyPaymentAsync` and validate the returned `VerifyPaymentResponse` against your expected transaction identifier, currency, and amount using the `IsValid` extension method.

## Mobile Money Payout

PayChangu.NET supports initiating Mobile Money payouts using `InitiateMobileMoneyPayoutAsync`. This allows your application to send funds to a recipient's mobile money account.

### Initiating a Mobile Money Payout

First, retrieve the supported mobile money operators using `GetMobileMoneyOperatorsAsync`. Use the selected operator's `RefId` when creating the payout request.

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

Create an `InitiateMobileMoneyPayoutRequest` and pass it to `InitiateMobileMoneyPayoutAsync`:

```csharp
var request = new InitiateMobileMoneyPayoutRequest
{
    Mobile = "+265992123456",
    MobileMoneyOperatorRefId = operatorRefId,
    Amount = 5000m,
    ChargeId = Guid.NewGuid().ToString(),
    Email = "recipient@example.com",
    FirstName = "John",
    LastName = "Banda"
};

var response = await _payChanguClient.InitiateMobileMoneyPayoutAsync(
    request,
    cancellationToken);

if (!response.Success || response.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect response.ErrorMessage and response.StatusCode.
    return;
}

var transaction = response.Result.Data;

// Access the payout transaction details.
var chargeId = transaction.ChargeId;
var referenceId = transaction.ReferenceId;
var status = transaction.Status;
var amount = transaction.Amount;
var currency = transaction.Currency;

// Access the mobile money operator details.
var operatorName = transaction.MobileMoney?.Name;
var operatorCountry = transaction.MobileMoney?.Country;
```

The request requires the recipient's mobile number, mobile money operator reference ID, payout amount, and a unique charge identifier. Recipient name and email details are optional.

The response provides the payout transaction details, including the PayChangu reference ID, transaction status, recipient mobile number, operator information, transaction charges, authorization details, and timestamps.

### Sandbox testing

The request supports an optional `TransactionStatus` property for simulating payout outcomes in PayChangu's sandbox environment.

Accepted values are `"failed"` and `"successful"`. Do not set this property when using the production environment.

**Important:** A successful API response does not necessarily mean the payout has been completed. Inspect the returned transaction status and handle payout notifications through your configured webhook endpoint. The SDK recognizes the `api.payout` webhook event for payout notifications.

## Bank Transfer Direct Charge

PayChangu.NET supports Bank Transfer Direct Charge payments. This flow generates bank account details that the customer can use to complete a payment.

### Initiating a Bank Transfer Direct Charge

Create an `InitiateBankTransferDirectChargeRequest` and pass it to `InitiateBankTransferDirectChargeAsync`.

```csharp
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
```

The response provides the bank name, account number, account name, account expiration timestamp, and transaction details.

The SDK sets the payment method to `mobile_bank_transfer`; you do not need to set it yourself. The currency defaults to `MWK`. `CreatePermanentAccount` is an optional nullable flag and defaults to `null` when omitted.

### Retrieving Bank Transfer Direct Charge Details

Use `GetSingleBankDirectChargeDetailsAsync` with the charge identifier to retrieve the transaction details:

```csharp
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
```

### Verifying the Transaction

Initiating a Bank Transfer Direct Charge generates the bank account details; it does not mean the customer has completed the payment.

Use `VerifyPaymentAsync` to verify the transaction:

```csharp
var verificationResponse = await _payChanguClient.VerifyPaymentAsync(
    chargeId,
    PayChanguPaymentFlow.DirectCharge,
    PayChanguPaymentMethod.BankTransfer,
    cancellationToken);

if (!verificationResponse.Success ||
    verificationResponse.Result == null ||
    !verificationResponse.Result.IsValid(chargeId, "MWK", 5000m))
{
    // The verification request failed or the payment details did not match.
    return;
}

// The transaction status, identifier, currency, and amount are valid.
```

**Important:** Always verify the transaction and confirm its final status before fulfilling an order. Do not treat the initiation response or the generated bank account details as proof of payment.

## Supported Banks and Payout Providers

PayChangu.NET provides `GetSupportedBanksAsync` to retrieve the banks and payout providers supported by PayChangu for a specified currency.

### Retrieving supported banks and payout providers

The currency defaults to `MWK`. You can provide another currency code when retrieving providers for a different currency.

```csharp
var response = await _payChanguClient.GetSupportedBanksAsync(
    "MWK",
    cancellationToken);

if (!response.Success || response.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect response.ErrorMessage and response.StatusCode.
    return;
}

var providers = response.Result.Data;

foreach (var provider in providers)
{
    var bankId = provider.Id;
    var bankName = provider.Name;
}
```

Each returned `SupportedBank` contains:

- `Id` — the provider UUID, required when initiating a Bank Payout.
- `Name` — the bank or payout provider name.

### Using a provider when initiating a payout

Use the provider UUID returned by `GetSupportedBanksAsync` as the `BankId` in an `InitiateBankPayoutRequest`.

```csharp
var banksResponse = await _payChanguClient.GetSupportedBanksAsync(
    "MWK",
    cancellationToken);

if (!banksResponse.Success || banksResponse.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    return;
}

var selectedBank = banksResponse.Result.Data
    .FirstOrDefault(x => x.Name == "National Bank of Malawi");

if (selectedBank == null)
{
    // The requested provider was not returned by the API.
    return;
}

var request = new InitiateBankPayoutRequest
{
    BankId = selectedBank.Id,
    Amount = 3000m,
    ChargeId = Guid.NewGuid().ToString(),
    BankAccountName = "Madalitso Kamwendo",
    BankAccountNumber = "1000000010"
};

// Pass the request to InitiateBankPayoutAsync.
```

**Note:** The returned list is filtered by currency. Use the UUID provided by PayChangu rather than hardcoding provider identifiers.

## Bank Payout

PayChangu.NET supports initiating bank payouts, retrieving individual bank payout details, and retrieving paginated bank payout transactions.

### Initiating a Bank Payout

Create an `InitiateBankPayoutRequest` and pass it to `InitiateBankPayoutAsync`.

The `BankId` property expects the UUID of the recipient's bank or payout provider. You can retrieve supported banks and payout providers using `GetSupportedBanksAsync`.

```csharp
var request = new InitiateBankPayoutRequest
{
    BankId = bankId,
    Amount = 3000m,
    ChargeId = Guid.NewGuid().ToString(),
    BankAccountName = "Madalitso Kamwendo",
    BankAccountNumber = "1000000010",
    Email = "recipient@example.com",
    FirstName = "Madalitso",
    LastName = "Kamwendo"
};

var response = await _payChanguClient.InitiateBankPayoutAsync(
    request,
    cancellationToken);

if (!response.Success || response.Result?.Data?.Transaction == null)
{
    // Handle the unsuccessful API response.
    // Inspect response.ErrorMessage and response.StatusCode.
    return;
}

var transaction = response.Result.Data.Transaction;

// Access the payout transaction details.
var chargeId = transaction.ChargeId;
var referenceId = transaction.ReferenceId;
var status = transaction.Status;
var amount = transaction.Amount;
var currency = transaction.Currency;

// Access the recipient's bank account details.
var bankName = transaction.RecipientAccountDetails.BankName;
var accountName = transaction.RecipientAccountDetails.AccountName;
var accountNumber = transaction.RecipientAccountDetails.AccountNumber;
```

The request uses `bank_transfer` as its default payout method. The amount is serialized as a JSON string. The recipient's email, first name, and last name are optional.

A successful initiation response means PayChangu accepted the payout request; it does not confirm that the payout has been completed. The returned transaction status can be used to track its progress.

### Retrieving an individual bank payout

Use `GetSingleBankPayoutDetailsAsync` with the payout's charge identifier to retrieve its transaction details.

```csharp
var detailsResponse = await _payChanguClient.GetSingleBankPayoutDetailsAsync(
    chargeId,
    cancellationToken);

if (!detailsResponse.Success || detailsResponse.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect detailsResponse.ErrorMessage and detailsResponse.StatusCode.
    return;
}

var transaction = detailsResponse.Result.Data;

var status = transaction.Status;
var amount = transaction.Amount;
var currency = transaction.Currency;
var referenceId = transaction.ReferenceId;

var bankName = transaction.RecipientAccountDetails.BankName;
var accountName = transaction.RecipientAccountDetails.AccountName;
var accountNumber = transaction.RecipientAccountDetails.AccountNumber;
```

The response provides the bank payout transaction, including its status, amount, currency, reference identifiers, transaction charges, and recipient account details.

### Retrieving bank payout transactions

Use `GetAllBankPayoutDetailsAsync` to retrieve bank payout transactions.

```csharp
var payoutsResponse = await _payChanguClient.GetAllBankPayoutDetailsAsync(
    cancellationToken);

if (!payoutsResponse.Success || payoutsResponse.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect payoutsResponse.ErrorMessage and payoutsResponse.StatusCode.
    return;
}

var pagination = payoutsResponse.Result.Data;

// Access pagination information.
var currentPage = pagination.CurrentPage;
var totalPages = pagination.TotalPages;
var pageSize = pagination.PerPage;
var nextPageUrl = pagination.NextPageUrl;

// Process transactions returned on the current page.
foreach (var transaction in pagination.Transactions)
{
    var chargeId = transaction.ChargeId;
    var status = transaction.Status;
    var amount = transaction.Amount;
    var bankName = transaction.RecipientAccountDetails.BankName;
}
```

The response includes the current page, total pages, page size, next page URL, and the transactions returned for that page.

**Important:** `GetAllBankPayoutDetailsAsync` retrieves only the page returned by the API. It does not automatically fetch subsequent pages. Use the pagination information to manage additional page retrieval separately.

**Payout notifications:** Bank payouts use the `api.payout` webhook event. Configure your webhook endpoint and process payout notifications to track transaction outcomes. Do not treat an accepted payout request or a pending transaction as confirmation that funds have reached the recipient.

## Card Direct Charge

PayChangu.NET supports Card Direct Charge payments, including transactions that require 3-D Secure (3DS) authentication.

### Initiating a Card Direct Charge

Create an `InitiateCardDirectChargeRequest` and pass it to `InitiateCardChargeAsync`.

```csharp
var chargeId = Guid.NewGuid().ToString();

var request = new InitiateCardDirectChargeRequest
{
    CardNumber = cardNumber,
    Expiry = cardExpiry,
    Cvv = cardCvv,
    CardholderName = "John Banda",
    Amount = 5000m,
    Currency = "MWK",
    ChargeId = chargeId,
    RedirectUrl = "https://example.com/payments/card/return",
    Email = "customer@example.com"
};

var response = await _payChanguClient.InitiateCardChargeAsync(
    request,
    cancellationToken);

if (!response.Success || response.Result == null)
{
    // Handle the unsuccessful API response.
    // Inspect response.ErrorMessage and response.StatusCode.
    return;
}

var chargeResponse = response.Result;

if (!chargeResponse.Success)
{
    // PayChangu did not accept the card charge.
    return;
}

if (chargeResponse.Requires3DSAuth &&
    !string.IsNullOrWhiteSpace(chargeResponse.ThreeDSAuthLink))
{
    // Redirect the customer to complete 3-D Secure authentication.
    return Redirect(chargeResponse.ThreeDSAuthLink);
}

// Continue with transaction verification.
```

The request includes the card details, amount, currency, charge identifier, redirect URL, and optional customer email.

### Handling 3-D Secure authentication

When `Requires3DSAuth` is `true`, redirect the customer to `ThreeDSAuthLink` to complete the authentication process.

PayChangu uses the supplied `RedirectUrl` to return the customer to your application after the authentication process.

If 3DS authentication is not required, proceed directly to transaction verification.

**Important:** A successful initiation response does not confirm that the payment has been completed. Do not fulfil an order based solely on the initiation response or the customer's return to your application.

### Verifying the card transaction

Use `VerifyPaymentAsync` with the charge identifier and the Card Direct Charge payment flow:

```csharp
var verificationResponse = await _payChanguClient.VerifyPaymentAsync(
    chargeId,
    PayChanguPaymentFlow.DirectCharge,
    PayChanguPaymentMethod.Card,
    cancellationToken);

if (!verificationResponse.Success ||
    !verificationResponse.Result.IsValid(chargeId, "MWK", 5000m))
{
    // The verification request failed or the payment details did not match.
    return;
}

// The transaction status, identifier, currency, and amount are valid.
// Continue with order fulfilment.
```

The `IsValid` extension method checks the transaction status, identifier, currency, and amount against your expected values.

### Refunding a card transaction

Use `RefundCardChargeAsync` with the charge identifier of the card transaction:

```csharp
var refundResponse = await _payChanguClient.RefundCardChargeAsync(
    chargeId,
    cancellationToken);

if (!refundResponse.Success)
{
    // Handle the unsuccessful refund request.
    // Inspect refundResponse.ErrorMessage and refundResponse.StatusCode.
    return;
}

// PayChangu accepted the refund request with HTTP 200.
```

The SDK considers the refund request successful when PayChangu returns HTTP 200. The response body is not deserialized.

**Important:** A successful refund request response should not automatically be treated as confirmation that the funds have been returned to the customer. Handle the refund according to the applicable PayChangu transaction and reconciliation process.

**Security:** Never log or expose card numbers or CVVs. Handle card details securely and avoid storing sensitive card verification data.

## Wallet Balance

PayChangu.NET provides `GetBalanceAsync` to retrieve wallet balance information for a specified currency.

### Retrieving the wallet balance

The currency defaults to `MWK`. You can also specify another currency, such as `USD`.

```csharp
var response = await _payChanguClient.GetBalanceAsync(
    cancellationToken: cancellationToken);

if (!response.Success || response.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    // Inspect response.ErrorMessage and response.StatusCode.
    return;
}

var balance = response.Result.Data;

// Access the wallet balance information.
var environment = balance.Environment;
var currency = balance.Currency;
var mainBalance = balance.MainBalance;
var collectionBalance = balance.CollectionBalance;
```

To retrieve the balance for a different currency:

```csharp
var response = await _payChanguClient.GetBalanceAsync(
    "USD",
    cancellationToken);

if (!response.Success || response.Result?.Data == null)
{
    // Handle the unsuccessful API response.
    return;
}

var balance = response.Result.Data;

var currency = balance.Currency;
var mainBalance = balance.MainBalance;
var collectionBalance = balance.CollectionBalance;
```

The returned `BalanceData` contains:

- `Environment` — the PayChangu environment associated with the wallet.
- `Currency` — the currency of the retrieved balance.
- `MainBalance` — the main wallet balance, returned as a string.
- `CollectionBalance` — the collection wallet balance, represented as a decimal.

**Note:** The method performs a balance retrieval request. It does not initiate a payout or transfer funds.

## Webhooks

PayChangu uses a single webhook endpoint for notifications from Standard Checkout, Direct Charge, and Payout transactions.

Configure your application's publicly accessible webhook endpoint in the PayChangu dashboard. This endpoint is separate from the `CallbackUrl` and `ReturnUrl` supplied when initiating a Standard Checkout payment.

PayChangu.NET provides `IPayChanguWebhookService` to validate webhook signatures and deserialize incoming notifications into a strongly typed `PayChanguWebhookEvent`.

When registering the SDK, configure both your API secret key and webhook secret:

```csharp
builder.Services.AddPayChangu(options =>
{
    options.SecretKey = builder.Configuration["PayChangu:SecretKey"]!;
    options.WebhookSecret = builder.Configuration["PayChangu:WebhookSecret"];
});
```

### Receiving and validating a webhook

Read the raw request body before parsing it. The signature must be verified against the original payload sent by PayChangu.

The following example uses an ASP.NET Core Minimal API endpoint:

```csharp
using System.Text;
using System.Text.Json;
using PayChangu.NET;

app.MapPost("/v1/webhooks/paychangu", async (
    HttpRequest request,
    IPayChanguWebhookService webhookService,
    CancellationToken cancellationToken) =>
{
    if (!request.Headers.TryGetValue("Signature", out var signature))
    {
        return Results.Unauthorized();
    }

    // Allow the request body to be read again if required.
    request.EnableBuffering();

    using var reader = new StreamReader(
        request.Body,
        Encoding.UTF8,
        leaveOpen: true);

    var payload = await reader.ReadToEndAsync(cancellationToken);

    // Reset the stream position after reading the raw payload.
    request.Body.Position = 0;

    if (!webhookService.VerifySignature(payload, signature.ToString()))
    {
        return Results.Unauthorized();
    }

    PayChanguWebhookEvent webhookEvent;

    try
    {
        // Parse the payload only after its signature has been validated.
        webhookEvent = webhookService.ParseWebhook(payload);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest();
    }
    catch (JsonException)
    {
        return Results.BadRequest();
    }

    // Process the verified webhook event in your application.
    // For example, pass it to your payment or order service.

    return Results.Ok();
});
```

The example assumes that `WebhookSecret` has been configured during SDK registration. The SDK's webhook service uses that configured secret; you do not need to retrieve the secret or implement HMAC signature verification yourself.

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
