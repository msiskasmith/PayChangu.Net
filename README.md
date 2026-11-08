# PayChangu.NET

A .NET SDK for integrating with the [PayChangu](https://paychangu.com/) payment gateway.

PayChangu.NET provides a strongly typed, asynchronous interface for interacting with PayChangu APIs from .NET applications. It simplifies payment initiation, transaction verification, direct charges, and payouts through a reusable client with built-in dependency injection support.

## Features

- Standard Checkout payment initiation.
- Mobile Money Direct Charge operations.
- Bank Transfer Direct Charge operations.
- Card Direct Charge operations, including refunds.
- Payment verification across supported payment flows.
- Mobile Money payout initiation and details retrieval.
- Bank payout initiation and transaction details retrieval.
- Retrieval of supported Mobile Money operators and payout banks.
- Strongly typed request and response models.
- Dependency injection support through `IHttpClientFactory`.
- Configurable API base URL.
- Asynchronous operations with cancellation token support.
- Standardized API response handling.
- Built on `HttpClient` and `System.Text.Json`.

## Installation

Install the package using the .NET CLI:

```bash
dotnet add package PayChangu.NET
```

## Configuration

Register PayChangu.NET with your application's dependency injection container.

```csharp
builder.Services.AddPayChangu(options =>
{
    options.SecretKey = builder.Configuration["PayChangu:SecretKey"]!;
});
```

You can also configure a custom API base URL:

```csharp
builder.Services.AddPayChangu(options =>
{
    options.SecretKey = builder.Configuration["PayChangu:SecretKey"]!;
    options.BaseUrl = "https://api.paychangu.com/";
});
```

For security, store your PayChangu secret key in configuration or a secure secrets provider rather than hardcoding it in your application.

## Using the Client

Inject `IPayChanguClient` into your application services or classes that need to communicate with PayChangu.

```csharp
public class PaymentService
{
    private readonly IPayChanguClient _payChanguClient;

    public PaymentService(IPayChanguClient payChanguClient)
    {
        _payChanguClient = payChanguClient;
    }
}
```

## API Responses

API operations return a `PayChanguApiResponse` or `PayChanguApiResponse<T>`, depending on whether the operation returns a response payload.

The response wrapper provides:

| Property | Description |
|---|---|
| `Success` | Indicates whether the API operation was successful. |
| `StatusCode` | HTTP status code returned by the API, when available. |
| `ErrorMessage` | Error information when the operation fails. |
| `Result` | Deserialized response payload for generic responses. |
| `RawJsonResponse` | Raw response body when available. |

Check the `Success` property before accessing the result:

```csharp
var response = await _payChanguClient.InitiatePaymentAsync(request);

if (response.Success)
{
    var payment = response.Result;

    // Continue with the successful response.
}
else
{
    Console.WriteLine(response.ErrorMessage);
}
```

## Cancellation

All asynchronous API operations support cancellation tokens.

```csharp
var response = await _payChanguClient.InitiatePaymentAsync(
    request,
    cancellationToken);
```

This allows your application to cancel outstanding HTTP requests when necessary.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
