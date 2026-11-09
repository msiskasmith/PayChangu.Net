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
- The Payment Flows page demonstrates Standard Checkout, Mobile Money Direct Charge, Bank Transfer Direct Charge, and Card Direct Charge with 3-D Secure authentication. The `/card-payment-return` page verifies a card transaction after the customer returns from authentication.
- The Payouts page demonstrates Mobile Money and bank payout initiation using the supported payout operators and banks.
- The Transactions page demonstrates payment verification. A successful Direct Charge initiation is not proof of payment; verify the transaction or process its webhook before fulfilling an order.
- The Webhooks page demonstrates the `POST /api/webhooks/paychangu` endpoint, including signature validation and parsing of Checkout, Direct Charge, and payout events. Accepted event details are kept in a bounded in-memory store (up to 100 events) and remain visible when navigating away from and back to the page. The history is cleared when the application restarts.
- To receive live PayChangu webhooks during local development, expose the local application through a secure public tunnel and configure that public URL as the webhook endpoint. The endpoint requires the `PayChangu:WebhookSecret` user secret.


## Card Direct Charge and PCI DSS

**PCI DSS compliance is mandatory for PayChangu Card Direct Charge.** PayChangu requires merchants to be PCI DSS compliant before processing direct card charges. After certification, contact PayChangu to have Card Direct Charge activated on your account. See the [PayChangu Card Direct Charge documentation](https://developer.paychangu.com/docs/card).

The Card Direct Charge example collects card number, expiry date, and CVV in the Blazor application and sends them to PayChangu through the server-side SDK. This means the application handles sensitive cardholder data.

- Do not use the Card Direct Charge example with real card details unless your business and application environment meet the applicable PCI DSS requirements and PayChangu has activated the feature on your account.
- Use PayChangu sandbox credentials and test card details for development.
- Never log or store the CVV. Do not log or unnecessarily retain card numbers or expiry details.
- The sample does not provide PCI DSS certification or make an application compliant.
- If you are not set up to handle cardholder data, use Standard Checkout so card details are entered on PayChangu's hosted checkout instead.

The Card Direct Charge flow redirects the customer to PayChangu's 3-D Secure authentication page. Set the return URL to your publicly accessible application URL ending in `/card-payment-return`. After the customer returns, the sample verifies the transaction with PayChangu; it does not trust the status supplied in the return URL.
