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
- Webhook signature validation support.

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

You can also configure the optional webhook secret:

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
- `IPayChanguWebhookService` — provides webhook signature validation and related webhook functionality.

Keep your secret keys in configuration or a secure secrets provider. Do not hardcode them in source code or expose them in client-side applications.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
