using PayChangu.NET;
using PayChangu.NET.BlazorSample.Components;
using PayChangu.NET.Extensions;
using PayChangu.NET.Models;
using PayChangu.NET.BlazorSample.Services;
using Radzen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddSingleton<WebhookEventStore>();

var secretKey = builder.Configuration["PayChangu:SecretKey"];

if (!string.IsNullOrWhiteSpace(secretKey))
{
    builder.Services.AddPayChangu(options =>
    {
        options.SecretKey = secretKey;
        options.WebhookSecret = builder.Configuration["PayChangu:WebhookSecret"];
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapPost("/api/webhooks/paychangu", async (
    HttpContext context,
    ILogger<Program> logger,
    WebhookEventStore eventStore) =>
{
    if (string.IsNullOrWhiteSpace(app.Configuration["PayChangu:SecretKey"]) ||
        string.IsNullOrWhiteSpace(app.Configuration["PayChangu:WebhookSecret"]))
    {
        return Results.Problem(
            "Configure both PayChangu:SecretKey and PayChangu:WebhookSecret to enable webhook validation.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
    var payload = await reader.ReadToEndAsync(context.RequestAborted);
    var signature = context.Request.Headers["Signature"].ToString();

    var webhookService = context.RequestServices
        .GetRequiredService<IPayChanguWebhookService>();

    if (!webhookService.VerifySignature(payload, signature))
    {
        logger.LogWarning("Rejected PayChangu webhook with an invalid signature.");
        return Results.Unauthorized();
    }

    PayChanguWebhookEvent webhookEvent;

    try
    {
        webhookEvent = webhookService.ParseWebhook(payload);
    }
    catch (System.Text.Json.JsonException ex)
    {
        logger.LogWarning(
            ex,
            "Rejected a signed PayChangu webhook with an invalid payload.");

        return Results.BadRequest();
    }

    string transactionIdentifier;

    try
    {
        transactionIdentifier = webhookEvent.TransactionIdentifier;
    }
    catch (System.Text.Json.JsonException ex)
    {
        logger.LogWarning(
            ex,
            "Rejected a signed PayChangu webhook without a usable transaction identifier.");

        return Results.BadRequest();
    }

    bool? paymentVerified = null;
    string? verificationStatus = null;
    string? verificationMessage = null;

    if (webhookEvent.PaymentFlow is PayChanguPaymentFlow.Checkout or PayChanguPaymentFlow.DirectCharge)
    {
        if (webhookEvent.PaymentMethod == PayChanguPaymentMethod.Unknown)
        {
            verificationStatus = "Not attempted";
            verificationMessage = "The payment method could not be determined from the webhook event.";

            logger.LogWarning(
                "Could not verify PayChangu payment {TransactionIdentifier} because its payment method is unknown.",
                transactionIdentifier);
        }
        else
        {
            var payChanguClient = context.RequestServices
                .GetRequiredService<IPayChanguClient>();

            var verificationResponse = await payChanguClient.Transactions.VerifyAsync(
                transactionIdentifier,
                webhookEvent.PaymentFlow,
                webhookEvent.PaymentMethod,
                context.RequestAborted);

            if (!verificationResponse.Success || verificationResponse.Result is null)
            {
                logger.LogError(
                    "PayChangu transaction verification failed for {TransactionIdentifier}. StatusCode: {StatusCode}, Error: {ErrorMessage}",
                    transactionIdentifier,
                    verificationResponse.StatusCode,
                    verificationResponse.ErrorMessage);

                // Return a server error so PayChangu can retry the webhook.
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }

            var verifiedPayment = verificationResponse.Result;

            var transactionMatches = string.Equals(
                verifiedPayment.TransactionIdentifier,
                transactionIdentifier,
                StringComparison.Ordinal);

            var currencyMatches = string.Equals(
                verifiedPayment.Currency,
                webhookEvent.Currency,
                StringComparison.OrdinalIgnoreCase);

            var amountMatches = verifiedPayment.Amount == webhookEvent.Amount;

            paymentVerified =
                string.Equals(
                    verifiedPayment.Status,
                    "success",
                    StringComparison.OrdinalIgnoreCase) &&
                transactionMatches &&
                currencyMatches &&
                amountMatches;

            verificationStatus = paymentVerified.Value
                ? "Verified"
                : !transactionMatches || !currencyMatches || !amountMatches
                    ? "Mismatch"
                    : verifiedPayment.Status;

            verificationMessage = paymentVerified.Value
                ? "The payment was confirmed by the PayChangu verification API."
                : !transactionMatches || !currencyMatches || !amountMatches
                    ? "The verification response did not match the transaction identifier, currency, or amount in the webhook."
                    : verifiedPayment.Message;

            if (paymentVerified.Value)
            {
                logger.LogInformation(
                    "PayChangu payment {TransactionIdentifier} was successfully verified.",
                    transactionIdentifier);
            }
            else
            {
                logger.LogWarning(
                    "PayChangu payment {TransactionIdentifier} was verified but is not confirmed as successful. Verification status: {VerificationStatus}",
                    transactionIdentifier,
                    verificationStatus);
            }
        }
    }
    else if (webhookEvent.PaymentFlow == PayChanguPaymentFlow.Payout)
    {
        verificationStatus = "Not applicable";
        verificationMessage = "Payout events are not processed as incoming payment transactions.";
    }
    else
    {
        logger.LogWarning(
            "Received an unsupported PayChangu webhook event type: {EventType}.",
            webhookEvent.EventType);

        return Results.BadRequest();
    }

    eventStore.Add(
        webhookEvent,
        paymentVerified,
        verificationStatus,
        verificationMessage);

    logger.LogInformation(
        "Received PayChangu webhook. EventType: {EventType}, Flow: {PaymentFlow}, Method: {PaymentMethod}, TransactionIdentifier: {TransactionIdentifier}, Status: {Status}, PaymentVerified: {PaymentVerified}",
        webhookEvent.EventType,
        webhookEvent.PaymentFlow,
        webhookEvent.PaymentMethod,
        transactionIdentifier,
        webhookEvent.Status,
        paymentVerified);

    return Results.Ok();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
