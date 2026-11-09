# PayChangu.NET Blazor Sample

A Blazor Web App demonstrating the [PayChangu.NET](https://www.nuget.org/packages/PayChangu.NET) SDK with Radzen Blazor components.

## Requirements

- .NET 10 SDK
- A PayChangu API secret key

## Configure the API key

The sample uses .NET User Secrets so the API secret key is kept out of source control.

From this project directory, run:

```bash
dotnet user-secrets set "PayChangu:SecretKey" "<your-paychangu-secret-key>"
```

If you plan to test webhook signature validation, also configure the webhook secret:

```bash
dotnet user-secrets set "PayChangu:WebhookSecret" "<your-webhook-secret>"
```

Do not commit API keys or webhook secrets to `appsettings.json` or source control.

## Run the sample

```bash
dotnet restore
dotnet run
```

Open the local URL shown in the application output.

## Notes

- The application uses Blazor Interactive Server rendering. PayChangu API calls and secret-key handling stay on the server.
- The PayChangu client is registered through `AddPayChangu` and can be injected into components or application services using `IPayChanguClient`.
- The Payment Flows page demonstrates Standard Checkout, Mobile Money Direct Charge, and Bank Transfer Direct Charge initiation.
- The Payouts page demonstrates Mobile Money and bank payout initiation using the supported payout operators and banks.
- The Transactions page demonstrates payment verification. A successful Direct Charge initiation is not proof of payment; verify the transaction or process its webhook before fulfilling an order.
- The Webhooks page demonstrates the `POST /api/webhooks/paychangu` endpoint, including signature validation and parsing of Checkout, Direct Charge, and payout events. The endpoint logs accepted event details to the application log.
- To receive live PayChangu webhooks during local development, expose the local application through a secure public tunnel and configure that public URL as the webhook endpoint. The endpoint requires the `PayChangu:WebhookSecret` user secret.
