using Microsoft.Extensions.DependencyInjection;
using PayChangu.NET.Extensions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PayChangu.NET.Tests;

public sealed class PayChanguWebhookServiceTests
{
    private const string BaseUrl = "https://api.paychangu.com/";
    private const string SecretKey = "test-secret-key";
    private const string WebhookSecret = "test-webhook-secret";

    private const string WebhookPayload = """
    {
        "event_type": "checkout.payment",
        "currency": "MWK",
        "amount": 5000,
        "charge": 200,
        "mode": "test",
        "type": "API Payment (Checkout)",
        "status": "success",
        "charge_id": "charge-12345",
        "reference": "provider-reference-123",
        "tx_ref": "test-transaction-001",
        "customer": {
            "customer_ref": "customer-123",
            "email": "customer@example.com",
            "first_name": "John",
            "last_name": "Banda",
            "phone": "+265991234567",
            "merchant_reference_identifier": "merchant-123"
        },
        "authorization": {
            "channel": "Mobile Money",
            "mobile_money": {
                "mobile_number": "+265991234567",
                "operator": "Airtel",
                "trans_id": "mobile-transaction-123"
            },
            "completed_at": "2026-11-04T18:30:00Z"
        },
        "created_at": "2026-11-04T18:25:00Z",
        "updated_at": "2026-11-04T18:30:00Z"
    }
    """;

    private static ServiceProvider CreateServiceProvider(
        string? webhookSecret = WebhookSecret)
    {
        var services = new ServiceCollection();

        services.AddPayChangu(options =>
        {
            options.SecretKey = SecretKey;
            options.BaseUrl = BaseUrl;
            options.WebhookSecret = webhookSecret;
        });

        return services.BuildServiceProvider();
    }

    private static string CreateSignature(
        string payload,
        string secret = WebhookSecret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);

        var hashBytes = hmac.ComputeHash(payloadBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    [Fact]
    public void VerifySignature_ReturnsTrue_WhenSignatureIsValid()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        var signature = CreateSignature(WebhookPayload);

        // Act
        var result = service.VerifySignature(WebhookPayload, signature);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifySignature_ReturnsTrue_WhenSignatureIsUppercase()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        var signature = CreateSignature(WebhookPayload).ToUpperInvariant();

        // Act
        var result = service.VerifySignature(WebhookPayload, signature);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifySignature_ReturnsFalse_WhenSignatureIsInvalid()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        const string invalidSignature =
            "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        var result = service.VerifySignature(
            WebhookPayload,
            invalidSignature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_ReturnsFalse_WhenWebhookSecretIsIncorrect()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        var signature = CreateSignature(
            WebhookPayload,
            "incorrect-webhook-secret");

        // Act
        var result = service.VerifySignature(WebhookPayload, signature);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "some-signature")]
    [InlineData("some-payload", "")]
    [InlineData("some-payload", null)]
    [InlineData(null, "some-signature")]
    public void VerifySignature_ReturnsFalse_WhenPayloadOrSignatureIsEmpty(
        string? payload,
        string? signature)
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        // Act
        var result = service.VerifySignature(payload!, signature!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_ReturnsFalse_WhenSignatureHasInvalidHexadecimalCharacters()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        const string invalidSignature =
            "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ";

        // Act
        var result = service.VerifySignature(
            WebhookPayload,
            invalidSignature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ParseWebhook_ReturnsWebhookEvent_WhenPayloadIsValid()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        // Act
        var result = service.ParseWebhook(WebhookPayload);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("checkout.payment", result.EventType);
        Assert.Equal("MWK", result.Currency);
        Assert.Equal(5000m, result.Amount);
        Assert.Equal(200m, result.Charge);
        Assert.Equal("test", result.Mode);
        Assert.Equal("success", result.Status);

        Assert.Equal(
            "test-transaction-001",
            result.TransactionIdentifier);

        Assert.Equal(
            "provider-reference-123",
            result.Reference);

        Assert.Equal("customer@example.com", result.Customer?.Email);
        Assert.Equal("John", result.Customer?.FirstName);

        Assert.Equal("Mobile Money", result.Source?.Channel);
        Assert.Equal("Airtel", result.Source?.MobileMoney?.Operator);
        Assert.Equal(
            "+265991234567",
            result.Source?.MobileMoney?.MobileNumber);
    }

    [Theory]
    [InlineData("200", 200)]
    [InlineData("\"200\"", 200)]
    [InlineData("\"\"", 0)]
    [InlineData("null", 0)]
    public void ParseWebhook_UsesDecimalConverterForCharge(
        string chargeJson,
        decimal expectedCharge)
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        var payload = WebhookPayload.Replace(
            "\"charge\": 200",
            "\"charge\": " + chargeJson);

        // Act
        var result = service.ParseWebhook(payload);

        // Assert
        Assert.Equal(expectedCharge, result.Charge);
    }

    [Fact]
    public void ParseWebhook_ThrowsJsonException_WhenPayloadIsInvalid()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        const string invalidPayload = "invalid-json";

        // Act & Assert
        Assert.Throws<JsonException>(
            () => service.ParseWebhook(invalidPayload));
    }

    [Fact]
    public void ParseWebhook_ThrowsJsonException_WhenPayloadIsNullJson()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        // Act & Assert
        Assert.Throws<JsonException>(
            () => service.ParseWebhook("null"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseWebhook_ThrowsArgumentException_WhenPayloadIsEmpty(
        string? payload)
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => service.ParseWebhook(payload!));
    }

    [Fact]
    public void WebhookService_ThrowsArgumentException_WhenWebhookSecretIsMissing()
    {
        // Arrange
        using var provider = CreateServiceProvider(null);

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => provider.GetRequiredService<IPayChanguWebhookService>());
    }

    [Fact]
    public void VerifySignature_ReturnsFalse_WhenPayloadHasBeenTamperedWith()
    {
        // Arrange
        using var provider = CreateServiceProvider();

        var service = provider.GetRequiredService<IPayChanguWebhookService>();

        var signature = CreateSignature(WebhookPayload);

        var tamperedPayload = WebhookPayload.Replace(
            "\"amount\": 5000",
            "\"amount\": 10000");

        // Act
        var result = service.VerifySignature(tamperedPayload, signature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AddPayChangu_DoesNotRequireWebhookSecret_DuringRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var exception = Record.Exception(() =>
            services.AddPayChangu(options =>
            {
                options.SecretKey = SecretKey;
                options.BaseUrl = BaseUrl;
                options.WebhookSecret = null;
            }));

        // Assert
        Assert.Null(exception);
    }
}
