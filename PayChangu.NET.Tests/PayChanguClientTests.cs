using Microsoft.Extensions.DependencyInjection;
using PayChangu.NET.Exceptions;
using PayChangu.NET.Models;
using PayChangu.NET.Tests.Fakes;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PayChangu.NET.Tests;

public sealed class PayChanguClientTests
{
    private const string BaseUrl = "https://api.paychangu.com/";
    private const string SecretKey = "test-secret-key";

    private static PaymentRequest CreatePaymentRequest()
    {
        return new PaymentRequest
        {
            Amount = 5000,
            Currency = "MWK",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Banda",
            CallbackUrl = "https://example.com/callback",
            ReturnUrl = "https://example.com/return",
            TransactionReference = "test-transaction-001",
            Customization = new PaymentCustomization
            {
                Title = "Product Purchase",
                Description = "Payment for product"
            },
            Meta = new PaymentMeta
            {
                Uuid = "test-uuid",
                Response = "test-response"
            }
        };
    }

    private static IPayChanguClient CreateClient(
    FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();

        services.AddPayChangu(
            options =>
            {
                options.SecretKey = SecretKey;
                options.BaseUrl = BaseUrl;
            },
            httpClient =>
            {
                httpClient.ConfigurePrimaryHttpMessageHandler(() => handler);
            });

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IPayChanguClient>();
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldReturnPaymentResponse_WhenSuccessful()
    {
        // Arrange
        var json = """
        {
            "message": "Hosted payment session generated successfully.",
            "status": "success",
            "data": {
                "event": "checkout.session:created",
                "checkout_url": "https://checkout.paychangu.com/123456",
                "data": {
                    "tx_ref": "test-transaction-001",
                    "currency": "MWK",
                    "amount": 5000,
                    "mode": "test",
                    "status": "pending"
                }
            }
        }
        """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var client = CreateClient(handler);

        // Act
        var result = await client.InitiatePaymentAsync(
            CreatePaymentRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.Status);
        Assert.Equal(
            "https://checkout.paychangu.com/123456",
            result.Data.CheckoutUrl);

        Assert.Equal(
            "test-transaction-001",
            result.Data.Session.TransactionReference);

        Assert.Equal(5000, result.Data.Session.Amount);
        Assert.Equal("pending", result.Data.Session.Status);
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldSendCorrectHttpRequest()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "message": "Success",
                        "status": "success",
                        "data": {
                            "event": "checkout.session:created",
                            "checkout_url": "https://checkout.paychangu.com/123",
                            "data": {
                                "tx_ref": "test-transaction-001",
                                "currency": "MWK",
                                "amount": 5000,
                                "mode": "test",
                                "status": "pending"
                            }
                        }
                    }
                    """)
            });

        var client = CreateClient(handler);

        // Act
        await client.InitiatePaymentAsync(CreatePaymentRequest());

        // Assert
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("payment", handler.RequestUri!.AbsolutePath.Trim('/'));

        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);

        Assert.NotNull(handler.RequestContent);

        using var document = JsonDocument.Parse(handler.RequestContent!);

        var root = document.RootElement;

        Assert.Equal(5000, root.GetProperty("amount").GetDecimal());
        Assert.Equal("MWK", root.GetProperty("currency").GetString());
        Assert.Equal(
            "test-transaction-001",
            root.GetProperty("tx_ref").GetString());

        Assert.Equal(
            "John",
            root.GetProperty("first_name").GetString());

        Assert.Equal(
            "Banda",
            root.GetProperty("last_name").GetString());

        Assert.True(root.TryGetProperty("callback_url", out _));
        Assert.True(root.TryGetProperty("return_url", out _));
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldThrowPayChanguException_WhenRequestFails()
    {
        // Arrange
        const string errorContent = """
        {
            "message": "Invalid API key",
            "status": "error"
        }
        """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorContent)
            });

        var client = CreateClient(handler);

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiatePaymentAsync(CreatePaymentRequest()));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(errorContent, exception.ResponseContent);
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldThrowPayChanguException_WhenResponseIsInvalid()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Invalid JSON")
            });

        var client = CreateClient(handler);

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiatePaymentAsync(CreatePaymentRequest()));

        // Assert
        Assert.Contains("Unable to deserialize", exception.Message);
    }
}