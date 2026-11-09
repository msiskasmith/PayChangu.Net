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

    bool? paymentVerified = null;
    string? verificationStatus = null;
    string? verificationMessage = null;

    if (webhookEvent.PaymentFlow is PayChanguPaymentFlow.Checkout or PayChanguPaymentFlow.DirectCharge)
    {
        var payChanguClient = context.RequestServices
                .GetRequiredService<IPayChanguClient>();

        var verificationResponse = await payChanguClient.Transactions.VerifyAsync(
            webhookEvent.TransactionIdentifier,
            webhookEvent.PaymentFlow,
            webhookEvent.PaymentMethod,
            context.RequestAborted);

        if (!verificationResponse.Success
                || verificationResponse.Result is null
                || !verificationResponse.Result
                        .IsValid(webhookEvent.TransactionIdentifier, webhookEvent.Currency, webhookEvent.Amount))
        {
            // Return a server error so PayChangu can retry the webhook.
            return Results.StatusCode(StatusCodes.Status502BadGateway);
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

    return Results.Ok();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
