using PayChangu.NET.BlazorSample.Components;
using PayChangu.NET.Extensions;
using PayChangu.NET.Models;
using Radzen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

var secretKey = builder.Configuration["PayChangu:SecretKey"];

if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException(
        "PayChangu:SecretKey is missing. Configure it using .NET User Secrets. " +
        "Run: dotnet user-secrets set \"PayChangu:SecretKey\" \"<your-secret-key>\"");
}

builder.Services.AddPayChangu(options =>
{
    options.SecretKey = secretKey;
    options.WebhookSecret = builder.Configuration["PayChangu:WebhookSecret"];
});

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
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(
        app.Configuration["PayChangu:WebhookSecret"]))
    {
        return Results.Problem(
            "Configure PayChangu:WebhookSecret to enable webhook validation.",
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
    catch (System.Text.Json.JsonException)
    {
        logger.LogWarning("Rejected a signed PayChangu webhook with an invalid payload.");
        return Results.BadRequest();
    }

    logger.LogInformation(
        "Received PayChangu webhook. EventType: {EventType}, Flow: {PaymentFlow}, Method: {PaymentMethod}, TransactionIdentifier: {TransactionIdentifier}, Status: {Status}",
        webhookEvent.EventType,
        webhookEvent.PaymentFlow,
        webhookEvent.PaymentMethod,
        webhookEvent.TransactionIdentifier,
        webhookEvent.Status);

    return Results.Ok();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
