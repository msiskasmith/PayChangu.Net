using Microsoft.Extensions.DependencyInjection;
using PayChangu.NET.Exceptions;
using PayChangu.NET.Models;
using PayChangu.NET.Tests.Fakes;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace PayChangu.NET.Tests;

public sealed class PayChanguClientTests
{
    private const string BaseUrl = "https://api.paychangu.com/";
    private const string SecretKey = "test-secret-key";

    private static InitiateCheckoutRequest CreatePaymentRequest()
    {
        return new InitiateCheckoutRequest
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

        using var serviceProvider = CreateServiceProvider(handler);

        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

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

        using var serviceProvider = CreateServiceProvider(handler);

        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiatePaymentAsync(CreatePaymentRequest()));

        // Assert
        Assert.Contains("Unable to deserialize", exception.Message);
    }

    [Fact]
    public async Task VerifyPaymentAsync_Checkout_ReturnsNormalizedResponseAndRawJson()
    {
        // Arrange
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

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.VerifyPaymentAsync(
            transactionReference,
            PayChanguPaymentFlow.Checkout,
            PayChanguPaymentMethod.Card);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(PayChanguPaymentFlow.Checkout, result.PaymentFlow);
        Assert.Equal(PayChanguPaymentMethod.Card, result.PaymentMethod);

        Assert.Equal(transactionReference, result.TransactionIdentifier);
        Assert.Equal(transactionReference, result.TransactionReference);

        Assert.Equal("26262633201", result.ProviderReference);
        Assert.Equal("checkout.payment", result.EventType);

        Assert.Equal("success", result.ApiStatus);
        Assert.Equal("success", result.Status);

        Assert.Equal(1, result.Attempts);
        Assert.Equal(1000m, result.Amount);
        Assert.Equal("MWK", result.Currency);

        Assert.NotNull(result.Charges);
        Assert.Equal("MWK", result.Charges.Currency);
        Assert.Equal(40m, result.Charges.Amount);

        Assert.Equal("MASTERCARD", result.Authorization!.Brand);
        Assert.Equal("customer@example.com", result.Customer!.Email);

        Assert.Equal(json, result.RawJsonResponse);
    }

    [Fact]
    public async Task VerifyPaymentAsync_DirectChargeMobileMoney_ReturnsNormalizedResponseAndRawJson()
    {
        // Arrange
        const string chargeId = "charge-001";

        const string json = """
    {
        "status": "success",
        "message": "Payment details retrieved successfully.",
        "data": {
            "charge_id": "charge-001",
            "ref_id": "ref-001",
            "trans_id": "trans-001",
            "currency": "MWK",
            "amount": 5000,
            "first_name": "John",
            "last_name": "Banda",
            "email": "john@example.com",
            "type": "Mobile Money",
            "trace_id": "trace-001",
            "status": "success",
            "mobile": "+265991234567",
            "attempts": 1,
            "mode": "live",
            "created_at": "2026-11-05T10:00:00Z",
            "completed_at": "2026-11-05T10:01:00Z",
            "event_type": "api.charge",
            "mobile_money": {
                "name": "Airtel Money",
                "ref_id": "operator-001",
                "country": "Malawi"
            },
            "transaction_charges": {
                "currency": "MWK",
                "amount": "100"
            },
            "authorization": {
                "channel": "Mobile Money",
                "provider": "Airtel Money",
                "mobile_number": "+265991234567",
                "completed_at": "2026-11-05T10:01:00Z"
            },
            "logs": []
        }
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.VerifyPaymentAsync(
            chargeId,
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.MobileMoney);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(PayChanguPaymentFlow.DirectCharge, result.PaymentFlow);
        Assert.Equal(PayChanguPaymentMethod.MobileMoney, result.PaymentMethod);

        Assert.Equal(chargeId, result.TransactionIdentifier);
        Assert.Equal(chargeId, result.ChargeId);

        Assert.Equal("ref-001", result.TransactionReference);
        Assert.Equal("trans-001", result.TransactionId);
        Assert.Equal("trace-001", result.TraceId);

        Assert.Equal("success", result.ApiStatus);
        Assert.Equal("success", result.Status);

        Assert.Equal(5000m, result.Amount);
        Assert.Equal("MWK", result.Currency);

        Assert.NotNull(result.Charges);
        Assert.Equal("MWK", result.Charges.Currency);
        Assert.Equal(100m, result.Charges.Amount);

        Assert.Equal("Airtel Money", result.MobileMoney!.Name);
        Assert.Equal("john@example.com", result.Customer!.Email);

        Assert.Equal(json, result.RawJsonResponse);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}mobile-money/payments/{chargeId}/verify"),
            handler.RequestUri);
    }

    [Fact]
    public async Task VerifyPaymentAsync_DirectChargeBankTransfer_ReturnsNormalizedResponseAndRawJson()
    {
        // Arrange
        const string chargeId = "bank-charge-001";

        const string json = """
    {
        "status": "success",
        "message": "Transaction details retrieved successfully.",
        "data": {
            "transaction": {
                "charge_id": "bank-charge-001",
                "ref_id": "bank-ref-001",
                "trans_id": "bank-trans-001",
                "currency": "MWK",
                "amount": 10000,
                "first_name": "Jane",
                "last_name": "Banda",
                "email": "jane@example.com",
                "type": "Bank Transfer",
                "trace_id": "bank-trace-001",
                "status": "success",
                "attempts": 1,
                "mode": "live",
                "created_at": "2026-11-05T10:00:00Z",
                "completed_at": "2026-11-05T10:05:00Z",
                "event_type": "api.charge",
                "transaction_charges": {
                    "currency": "MWK",
                    "amount": "200"
                },
                "authorization": {
                    "channel": "Bank Transfer",
                    "payer_bank": "Test Bank",
                    "payer_account_number": "1234567890",
                    "payer_account_name": "Jane Banda"
                },
                "logs": []
            }
        }
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.VerifyPaymentAsync(
            chargeId,
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.BankTransfer);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(PayChanguPaymentFlow.DirectCharge, result.PaymentFlow);
        Assert.Equal(PayChanguPaymentMethod.BankTransfer, result.PaymentMethod);

        Assert.Equal(chargeId, result.TransactionIdentifier);
        Assert.Equal("bank-ref-001", result.TransactionReference);
        Assert.Equal("bank-trans-001", result.TransactionId);

        Assert.Equal("success", result.ApiStatus);
        Assert.Equal("success", result.Status);

        Assert.Equal(10000m, result.Amount);
        Assert.Equal("MWK", result.Currency);

        Assert.NotNull(result.Charges);
        Assert.Equal(200m, result.Charges.Amount);

        Assert.Equal("Test Bank", result.Authorization!.PayerBank);
        Assert.Equal("1234567890", result.Authorization.PayerAccountNumber);
        Assert.Equal("Jane Banda", result.Authorization.PayerAccountName);

        Assert.Equal(json, result.RawJsonResponse);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}direct-charge/transactions/{chargeId}/details"),
            handler.RequestUri);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenApiReturnsError_ThrowsPayChanguException()
    {
        // Arrange
        const string errorContent = """{"message":"Transaction not found."}""";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(errorContent)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyPaymentAsync(
                "TXN-12345",
                PayChanguPaymentFlow.Checkout,
                PayChanguPaymentMethod.Card));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(errorContent, exception.ResponseContent);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenResponseIsInvalidJson_ThrowsPayChanguException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid-json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyPaymentAsync(
                "TXN-12345",
                PayChanguPaymentFlow.Checkout,
                PayChanguPaymentMethod.Card));

        // Assert
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyPaymentAsync_WhenIdentifierIsInvalid_ThrowsArgumentException(
        string? transactionIdentifier)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => client.VerifyPaymentAsync(
                transactionIdentifier!,
                PayChanguPaymentFlow.Checkout,
                PayChanguPaymentMethod.Card));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentFlowIsUnsupported_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.VerifyPaymentAsync(
                "TXN-12345",
                (PayChanguPaymentFlow)999,
                PayChanguPaymentMethod.Card));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentMethodIsUnsupported_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.VerifyPaymentAsync(
                "TXN-12345",
                PayChanguPaymentFlow.Checkout,
                (PayChanguPaymentMethod)999));
    }

    [Fact]
    public async Task VerifyPaymentAsync_DirectChargeCard_ThrowsNotSupportedException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => client.VerifyPaymentAsync(
                "charge-001",
                PayChanguPaymentFlow.DirectCharge,
                PayChanguPaymentMethod.Card));
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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyDirectChargeRequest
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
        var result = await client.InitiateMobileMoneyDirectChargeAsync(request);

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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        await client.InitiateMobileMoneyDirectChargeAsync(request);

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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyDirectChargeRequest
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
        await client.InitiateMobileMoneyDirectChargeAsync(request);

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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        await client.InitiateMobileMoneyDirectChargeAsync(request);

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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "invalid-operator",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiateMobileMoneyDirectChargeAsync(request));

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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyDirectChargeRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = "5000",
            ChargeId = "charge-001"
        };

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiateMobileMoneyDirectChargeAsync(request));

        // Assert
        Assert.NotNull(exception.InnerException);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.InitiateMobileMoneyDirectChargeAsync(null!));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenCancellationIsRequested_CancelsHttpRequest()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();

        var handler = new FakeHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(
                    Timeout.Infinite,
                    cancellationToken);

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        using var serviceProvider = CreateServiceProvider(handler);

        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var verificationTask = client.VerifyPaymentAsync(
            "TXN-12345",
            PayChanguPaymentFlow.Checkout,
            PayChanguPaymentMethod.Card,
            cancellationTokenSource.Token);

        await handler.RequestReceived.Task;

        await cancellationTokenSource.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => verificationTask);

        Assert.True(handler.ReceivedCancellationToken.IsCancellationRequested);
    }

    private static ServiceProvider CreateServiceProvider(
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

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenTransactionChargeAmountIsInvalid_ThrowsPayChanguException()
    {
        // Arrange
        const string json = """
    {
        "status": "success",
        "message": "Payment details retrieved successfully.",
        "data": {
            "charge_id": "charge-001",
            "ref_id": "ref-001",
            "trans_id": "trans-001",
            "currency": "MWK",
            "amount": 5000,
            "status": "success",
            "transaction_charges": {
                "currency": "MWK",
                "amount": "invalid"
            },
            "logs": []
        }
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        using var serviceProvider = CreateServiceProvider(handler);

        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyPaymentAsync(
                "charge-001",
                PayChanguPaymentFlow.DirectCharge,
                PayChanguPaymentMethod.MobileMoney));

        // Assert
        Assert.Contains(
            "invalid transaction charge amount",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyPaymentAsync_CheckoutWithMissingData_ThrowsPayChanguException()
    {
        // Arrange
        const string responseContent =
            """
        {
            "status": "success",
            "message": "OK",
            "data": null
        }
        """;

        var handler = new FakeHttpMessageHandler(
    new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    responseContent,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyPaymentAsync(
                "test-reference",
                PayChanguPaymentFlow.Checkout,
                PayChanguPaymentMethod.MobileMoney));

        // Assert
        Assert.Contains("empty Checkout verification data payload", exception.Message);
    }

    [Fact]
    public async Task VerifyPaymentAsync_MobileMoneyDirectChargeWithMissingData_ThrowsPayChanguException()
    {
        // Arrange
        const string responseContent =
            """
        {
            "status": "success",
            "message": "OK",
            "data": null
        }
        """;

        var handler = new FakeHttpMessageHandler(
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                responseContent,
                Encoding.UTF8,
                "application/json")
        });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyPaymentAsync(
                "test-charge-id",
                PayChanguPaymentFlow.DirectCharge,
                PayChanguPaymentMethod.MobileMoney));

        // Assert
        Assert.Contains(
            "empty Mobile Money Direct Charge verification data payload",
            exception.Message);
    }

    [Fact]
    public async Task VerifyPaymentAsync_BankTransferDirectChargeWithMissingTransaction_ThrowsPayChanguException()
    {
        // Arrange
        const string responseContent =
            """
        {
            "status": "success",
            "message": "OK",
            "data": {
                "transaction": null
            }
        }
        """;

        var handler = new FakeHttpMessageHandler(
    new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                responseContent,
                Encoding.UTF8,
                "application/json")
        });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.VerifyPaymentAsync(
                "test-charge-id",
                PayChanguPaymentFlow.DirectCharge,
                PayChanguPaymentMethod.BankTransfer));

        // Assert
        Assert.Contains(
            "empty Bank Transfer Direct Charge verification transaction payload",
            exception.Message);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        const string json = """
    {
        "status": "success",
        "message": "Payment initialized successfully.",
        "data": {
            "payment_account_details": {
                "bank_name": "Centenary Bank",
                "account_number": "2652493369",
                "account_name": "PayChangu Dev",
                "account_expiration_timestamp": 1739830475
            },
            "transaction": {
                "charge_id": "PTC12383",
                "ref_id": "75513659949",
                "trans_id": null,
                "currency": "MK",
                "amount": 1020,
                "first_name": null,
                "last_name": null,
                "email": null,
                "type": "Direct API Payment",
                "trace_id": "v0atWHs0NLEN3vbTAL4DKFLm59dVHuRM",
                "status": "pending",
                "mobile": "0",
                "attempts": 1,
                "mode": "live",
                "created_at": "2025-02-17T21:14:35.000000Z",
                "completed_at": null,
                "event_type": "api.charge.payment",
                "transaction_charges": {
                    "currency": "MK",
                    "amount": "20"
                },
                "authorization": {
                    "channel": "Mobile Bank Transfer",
                    "card_number": null,
                    "expiry": null,
                    "brand": null,
                    "provider": null,
                    "mobile_number": null,
                    "payer_bank": null,
                    "payer_account_number": null,
                    "payer_account_name": null,
                    "completed_at": null
                },
                "logs": [
                    {
                        "type": "log",
                        "message": "Attempted to pay with bank transfer",
                        "created_at": "2025-02-17T21:14:35.000000Z"
                    }
                ]
            }
        }
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateBankTransferDirectChargeRequest
        {
            Amount = "1020",
            ChargeId = "PTC12383",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Banda",
            Mobile = "0991234567"
        };

        // Act
        var result = await client.InitiateBankTransferDirectChargeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.Status);
        Assert.Equal("Payment initialized successfully.", result.Message);

        Assert.Equal("Centenary Bank", result.Data.PaymentAccountDetails.BankName);
        Assert.Equal("2652493369", result.Data.PaymentAccountDetails.AccountNumber);
        Assert.Equal("PayChangu Dev", result.Data.PaymentAccountDetails.AccountName);
        Assert.Equal(
            1739830475L,
            result.Data.PaymentAccountDetails.AccountExpirationTimestamp);

        Assert.Equal("PTC12383", result.Data.Transaction.ChargeId);
        Assert.Equal("75513659949", result.Data.Transaction.ReferenceId);
        Assert.Equal(1020m, result.Data.Transaction.Amount);
        Assert.Equal("MK", result.Data.Transaction.Currency);
        Assert.Equal("pending", result.Data.Transaction.Status);

        Assert.Equal(
            "Mobile Bank Transfer",
            result.Data.Transaction.Authorization!.Channel);

        Assert.Single(result.Data.Transaction.Logs);
        Assert.Equal(
            "Attempted to pay with bank transfer",
            result.Data.Transaction.Logs[0].Message);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_UsesCorrectEndpoint()
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

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateBankTransferDirectChargeRequest
        {
            Amount = "5000",
            ChargeId = "bank-charge-001"
        };

        // Act
        await client.InitiateBankTransferDirectChargeAsync(request);

        // Assert
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}direct-charge/payments/initialize"),
            handler.RequestUri);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_SerializesRequestCorrectly()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Request initiated","data":{}}""")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateBankTransferDirectChargeRequest
        {
            Amount = "5000",
            ChargeId = "bank-charge-001",
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Banda",
            Mobile = "0991234567",
            CreatePermanentAccount = true
        };

        // Act
        await client.InitiateBankTransferDirectChargeAsync(request);

        // Assert
        Assert.NotNull(handler.RequestContent);

        using var document = JsonDocument.Parse(handler.RequestContent);
        var root = document.RootElement;

        Assert.Equal("5000", root.GetProperty("amount").GetString());
        Assert.Equal("MWK", root.GetProperty("currency").GetString());
        Assert.Equal("mobile_bank_transfer", root.GetProperty("payment_method").GetString());
        Assert.Equal("bank-charge-001", root.GetProperty("charge_id").GetString());
        Assert.Equal("john@example.com", root.GetProperty("email").GetString());
        Assert.Equal("John", root.GetProperty("first_name").GetString());
        Assert.Equal("Banda", root.GetProperty("last_name").GetString());
        Assert.Equal("0991234567", root.GetProperty("mobile").GetString());
        Assert.True(root.GetProperty("create_permanent_account").GetBoolean());
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_UsesBearerAuthentication()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Request initiated","data":{}}""")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateBankTransferDirectChargeRequest
        {
            Amount = "5000",
            ChargeId = "bank-charge-001"
        };

        // Act
        await client.InitiateBankTransferDirectChargeAsync(request);

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_ThrowsPayChanguException_WhenRequestFails()
    {
        // Arrange
        const string errorContent = """{"message":"Invalid charge request"}""";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorContent)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateBankTransferDirectChargeRequest
        {
            Amount = "5000",
            ChargeId = "bank-charge-001"
        };

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiateBankTransferDirectChargeAsync(request));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(errorContent, exception.ResponseContent);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_ThrowsPayChanguException_WhenResponseIsInvalidJson()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid-json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateBankTransferDirectChargeRequest
        {
            Amount = "5000",
            ChargeId = "bank-charge-001"
        };

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.InitiateBankTransferDirectChargeAsync(request));

        // Assert
        Assert.NotNull(exception.InnerException);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.InitiateBankTransferDirectChargeAsync(null!));
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsBalance_WhenRequestSucceeds()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                {
                  "status": "success",
                  "message": "Wallet balances retrieved successfully",
                  "data": {
                    "environment": "live",
                    "currency": "MWK",
                    "main_balance": "953037.95",
                    "collection_balance": 0
                  }
                }
                """,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var result = await client.GetBalanceAsync();

        Assert.Equal("success", result.Status);
        Assert.Equal("Wallet balances retrieved successfully", result.Message);
        Assert.Equal("live", result.Data.Environment);
        Assert.Equal("MWK", result.Data.Currency);
        Assert.Equal("953037.95", result.Data.MainBalance);
        Assert.Equal(0m, result.Data.CollectionBalance);
    }

    [Fact]
    public async Task GetBalanceAsync_UsesCorrectEndpoint()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Success","data":{"environment":"live","currency":"MWK","main_balance":"100","collection_balance":0}}""")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        await client.GetBalanceAsync();

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            "https://api.paychangu.com/wallet-balance?currency=MWK",
            handler.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetBalanceAsync_UsesSpecifiedCurrency()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Success","data":{"environment":"live","currency":"USD","main_balance":"100","collection_balance":0}}""")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var result = await client.GetBalanceAsync("USD");

        Assert.Equal(
            "https://api.paychangu.com/wallet-balance?currency=USD",
            handler.RequestUri!.ToString());

        Assert.Equal("USD", result.Data.Currency);
    }

    [Fact]
    public async Task GetBalanceAsync_SendsBearerAuthentication()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Success","data":{"environment":"live","currency":"MWK","main_balance":"100","collection_balance":0}}""")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        await client.GetBalanceAsync();

        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task GetBalanceAsync_ThrowsPayChanguException_WhenRequestFails()
    {
        const string errorContent = """{"status":"failed","message":"Unauthorized"}""";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorContent)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.GetBalanceAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(errorContent, exception.ResponseContent);
    }

    [Fact]
    public async Task GetBalanceAsync_ThrowsPayChanguException_WhenResponseIsInvalidJson()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.GetBalanceAsync());

        Assert.IsType<JsonException>(exception.InnerException);
    }

}