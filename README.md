# PayChangu.NET

A .NET SDK for integrating with the [PayChangu](https://paychangu.com/) payment gateway.

PayChangu.NET provides a simple, typed interface for interacting with PayChangu APIs from .NET applications.

## Features

- Standard Checkout payment initiation.
- Strongly typed request and response models.
- Dependency injection support.
- Configurable API base URL.
- Cancellation token support.
- Typed exceptions for API errors.
- Built on `HttpClient` and `System.Text.Json`.

## Installation

```bash
dotnet add package PayChangu.NET
```

## Configuration

Register the client using dependency injection:

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

## Initiating a payment

Inject `IPayChanguClient` into your service or application class:

```csharp
public class PaymentService
{
    private readonly IPayChanguClient _payChanguClient;

    public PaymentService(IPayChanguClient payChanguClient)
    {
        _payChanguClient = payChanguClient;
    }

    public async Task<PaymentResponse> InitiatePaymentAsync()
    {
        var request = new PaymentRequest
        {
            Amount = 5000,
            Currency = "MWK",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Banda",
            CallbackUrl = "https://example.com/callback",
            ReturnUrl = "https://example.com/return",
            TransactionReference = Guid.NewGuid().ToString(),
            Customization = new PaymentCustomization
            {
                Title = "Product Purchase",
                Description = "Payment for your purchase"
            },
            Meta = new PaymentMeta
            {
                Uuid = Guid.NewGuid().ToString(),
                Response = "Payment metadata"
            }
        };

        return await _payChanguClient.InitiatePaymentAsync(request);
    }
}
```

The returned `PaymentResponse` contains the checkout URL. Redirect the customer to that URL to complete payment.

## Error handling

The SDK throws `PayChanguException` when PayChangu returns an unsuccessful HTTP response or an invalid response.

```csharp
try
{
    var response = await _payChanguClient.InitiatePaymentAsync(request);

    var checkoutUrl = response.Data.CheckoutUrl;
}
catch (PayChanguException ex)
{
    // Handle PayChangu API errors.
    Console.WriteLine(ex.Message);
}
```

## Cancellation

All asynchronous API operations support cancellation tokens.

```csharp
var response = await _payChanguClient.InitiatePaymentAsync(
    request,
    cancellationToken);
```

## Current API support

| Feature | Availability |
|---|---|
| Standard Checkout | Available |
| Transaction verification | Planned |
| Additional PayChangu operations | Planned |

## License

License information will be added before the first public release.
