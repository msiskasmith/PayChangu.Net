using Microsoft.Extensions.DependencyInjection;
using PayChangu.NET.Exceptions;
using PayChangu.NET.Models;
using PayChangu.NET.Tests.Fakes;
using System.Net;
using System.Text;
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
        (_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));

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
    (_, _) => Task.FromResult(
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
        }));

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

    [Fact]
    public async Task VerifyTransactionAsync_ReturnsVerificationResponse()
    {
        const string transactionReference = "TXN-12345";

        const string json = """
    {
        "status": "success",
        "message": "Payment details retrieved successfully.",
        "data": {
            "event_type": "checkout.payment",
            "tx_ref": "TXN-12345",
            "mode": "live",
            "type": "API Payment (Checkout)",
            "status": "success",
            "number_of_attempts": 1,
            "reference": "26262633201",
            "currency": "MWK",
            "amount": 1000,
            "charges": 40,
            "customization": {
                "title": "Test Payment",
                "description": "Payment Description",
                "logo": null
            },
            "meta": null,
            "authorization": {
                "channel": "Card",
                "card_number": "230377******0408",
                "expiry": "2035-12",
                "brand": "MASTERCARD",
                "provider": null,
                "mobile_number": null,
                "completed_at": "2024-08-08T23:21:22.000000Z"
            },
            "customer": {
                "email": "customer@example.com",
                "first_name": "John",
                "last_name": "Banda"
            },
            "logs": [],
            "created_at": "2024-08-08T23:20:21.000000Z",
            "updated_at": "2024-08-08T23:20:21.000000Z"
        }
    }
    """;

        var handler = new FakeHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }
        };

        var client = CreateClient(handler);

        var result = await client.VerifyTransactionAsync(transactionReference);

        Assert.NotNull(result);
        Assert.Equal("success", result.Status);
        Assert.Equal("TXN-12345", result.Data.TransactionReference);
        Assert.Equal("success", result.Data.Status);
        Assert.Equal(1000m, result.Data.Amount);
        Assert.Equal("MWK", result.Data.Currency);
        Assert.Equal("26262633201", result.Data.ProviderReference);
        Assert.Equal("MASTERCARD", result.Data.Authorization!.Brand);
    }

    [Fact]
    public async Task VerifyTransactionAsync_UsesCorrectEndpoint()
    {
        const string transactionReference = "TXN-12345";

        var handler = new FakeHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"OK","data":{"status":"success"}}""")
            }
        };

        var client = CreateClient(handler);

        await client.VerifyTransactionAsync(transactionReference);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);

        Assert.Equal(
            $"verify-payment/{transactionReference}",
            handler.LastRequest.RequestUri!.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task VerifyTransactionAsync_WhenApiReturnsError_ThrowsPayChanguException()
    {
        var handler = new FakeHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Transaction not found.")
            }
        };

        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyTransactionAsync("TXN-12345"));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("Transaction not found.", exception.ResponseContent);
    }

    [Fact]
    public async Task VerifyTransactionAsync_WhenResponseIsInvalidJson_ThrowsPayChanguException()
    {
        var handler = new FakeHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid-json")
            }
        };

        var client = CreateClient(handler);

        await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyTransactionAsync("TXN-12345"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyTransactionAsync_WhenReferenceIsInvalid_ThrowsArgumentException(
    string? transactionReference)
    {
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.VerifyTransactionAsync(transactionReference!));
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                {
                    "status": "success",
                    "message": "Payment request initiated successfully.",
                    "data": {
                        "amount": 5000,
                        "charge_id": "charge-001",
                        "ref_id": "ref-001",
                        "trans_id": null,
                        "first_name": "John",
                        "last_name": "Banda",
                        "email": "john@example.com",
                        "status": "pending",
                        "mobile": "+265991234567",
                        "attempts": 1,
                        "currency": "MWK",
                        "mode": "test",
                        "created_at": "2026-11-05T10:00:00Z",
                        "completed_at": null,
                        "mobile_money": {
                            "name": "Airtel Money",
                            "ref_id": "operator-001",
                            "country": "Malawi"
                        }
                    }
                }
                """,
                    Encoding.UTF8,
                    "application/json")
            });

        var client = CreateClient(handler);

        var request = new MobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001",
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Banda"
        };

        // Act
        var result = await client.InitiateMobileMoneyChargeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.Status);
        Assert.Equal("Payment request initiated successfully.", result.Message);
        Assert.Equal("charge-001", result.Data.ChargeId);
        Assert.Equal("ref-001", result.Data.ReferenceId);
        Assert.Equal("pending", result.Data.Status);
        Assert.Equal(5000m, result.Data.Amount);
        Assert.Equal("Airtel Money", result.Data.MobileMoney.Name);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_UsesCorrectEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Request initiated","data":{}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var client = CreateClient(handler);

        var request = new MobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        await client.InitiateMobileMoneyChargeAsync(request);

        // Assert
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(
            new Uri("https://api.paychangu.com/mobile-money/payments/initialize"),
            handler.RequestUri);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_SerializesRequestCorrectly()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Request initiated","data":{}}""")
    
            });

        var client = CreateClient(handler);

        var request = new MobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001",
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Banda"
        };

        // Act
        await client.InitiateMobileMoneyChargeAsync(request);

        // Assert
        Assert.NotNull(handler.RequestContent);

        using var document = JsonDocument.Parse(handler.RequestContent);
        var root = document.RootElement;

        Assert.Equal("+265991234567", root.GetProperty("mobile").GetString());
        Assert.Equal("operator-001", root.GetProperty("mobile_money_operator_ref_id").GetString());
        Assert.Equal("5000", root.GetProperty("amount").GetString());
        Assert.Equal("charge-001", root.GetProperty("charge_id").GetString());
        Assert.Equal("john@example.com", root.GetProperty("email").GetString());
        Assert.Equal("John", root.GetProperty("first_name").GetString());
        Assert.Equal("Banda", root.GetProperty("last_name").GetString());
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_UsesBearerAuthentication()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Request initiated","data":{}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var client = CreateClient(handler);

        var request = new MobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        await client.InitiateMobileMoneyChargeAsync(request);

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("test-secret-key", handler.AuthorizationParameter);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_ThrowsPayChanguException_WhenRequestFails()
    {
        // Arrange
        const string errorContent = """{"message":"Invalid operator"}""";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorContent)
            });

        var client = CreateClient(handler);

        var request = new MobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "invalid-operator",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiateMobileMoneyChargeAsync(request));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(errorContent, exception.ResponseContent);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_ThrowsPayChanguException_WhenResponseIsInvalidJson()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid-json")
            });

        var client = CreateClient(handler);

        var request = new MobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiateMobileMoneyChargeAsync(request));

        // Assert
        Assert.NotNull(exception.InnerException);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.InitiateMobileMoneyChargeAsync(null!));
    }
}