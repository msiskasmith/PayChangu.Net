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
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);
        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Hosted payment session generated successfully.",
            result.Result.Message);

        Assert.Equal(
            "https://checkout.paychangu.com/123456",
            result.Result.Data.CheckoutUrl);

        Assert.Equal(
            "test-transaction-001",
            result.Result.Data.Session.TransactionReference);

        Assert.Equal(5000, result.Result.Data.Session.Amount);
        Assert.Equal("pending", result.Result.Data.Session.Status);
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldReturnFailureResponse_WhenRequestFails()
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
        var result = await client.InitiatePaymentAsync(CreatePaymentRequest());

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(errorContent, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task InitiatePaymentAsync_ShouldReturnFailureResponse_WhenResponseIsInvalid()
    {
        // Arrange
        const string invalidJson = "Invalid JSON";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.InitiatePaymentAsync(CreatePaymentRequest());

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);
        Assert.Equal(invalidJson, result.RawJsonResponse);
        Assert.Null(result.Result);
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
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);

        Assert.Equal(
            PayChanguPaymentFlow.Checkout,
            result.Result.PaymentFlow);

        Assert.Equal(
            PayChanguPaymentMethod.Card,
            result.Result.PaymentMethod);

        Assert.Equal(
            transactionReference,
            result.Result.TransactionIdentifier);

        Assert.Equal(
            transactionReference,
            result.Result.TransactionReference);

        Assert.Equal("26262633201", result.Result.ProviderReference);
        Assert.Equal("checkout.payment", result.Result.EventType);

        Assert.Equal("success", result.Result.ApiStatus);
        Assert.Equal("success", result.Result.Status);

        Assert.Equal(1, result.Result.Attempts);
        Assert.Equal(1000m, result.Result.Amount);
        Assert.Equal("MWK", result.Result.Currency);

        Assert.NotNull(result.Result.Charges);
        Assert.Equal("MWK", result.Result.Charges.Currency);
        Assert.Equal(40m, result.Result.Charges.Amount);

        Assert.Equal("MASTERCARD", result.Result.Authorization!.Brand);
        Assert.Equal("customer@example.com", result.Result.Customer!.Email);
    }

    [Fact]
    public async Task VerifyPaymentAsync_ShouldReturnFailureResponse_WhenApiReturnsError()
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
        var result = await client.VerifyPaymentAsync(
            "TXN-12345",
            PayChanguPaymentFlow.Checkout,
            PayChanguPaymentMethod.Card);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(errorContent, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task VerifyPaymentAsync_ShouldReturnFailureResponse_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string invalidJson = "invalid-json";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.VerifyPaymentAsync(
            "TXN-12345",
            PayChanguPaymentFlow.Checkout,
            PayChanguPaymentMethod.Card);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);
        Assert.Equal(invalidJson, result.RawJsonResponse);
        Assert.Null(result.Result);
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
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);

        Assert.Equal(
            PayChanguPaymentFlow.DirectCharge,
            result.Result.PaymentFlow);

        Assert.Equal(
            PayChanguPaymentMethod.MobileMoney,
            result.Result.PaymentMethod);

        Assert.Equal(chargeId, result.Result.TransactionIdentifier);
        Assert.Equal(chargeId, result.Result.ChargeId);

        Assert.Equal("ref-001", result.Result.TransactionReference);
        Assert.Equal("trans-001", result.Result.TransactionId);
        Assert.Equal("trace-001", result.Result.TraceId);

        Assert.Equal("success", result.Result.ApiStatus);
        Assert.Equal("success", result.Result.Status);

        Assert.Equal(5000m, result.Result.Amount);
        Assert.Equal("MWK", result.Result.Currency);

        Assert.NotNull(result.Result.Charges);
        Assert.Equal("MWK", result.Result.Charges.Currency);
        Assert.Equal(100m, result.Result.Charges.Amount);

        Assert.Equal("Airtel Money", result.Result.MobileMoney!.Name);
        Assert.Equal("john@example.com", result.Result.Customer!.Email);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}mobile-money/payments/{chargeId}/verify"),
            handler.RequestUri);
    }

    [Fact]
    public async Task GetMobileMoneyOperatorsAsync_ReturnsOperators_WhenRequestSucceeds()
    {
        const string responseContent = """
        {
          "status": "success",
          "message": "Supported mobile money operators retrieved successfully.",
          "data": [
            {
              "id": 1,
              "name": "TNM Mpamba",
              "ref_id": "27494cb5-ba9e-437f-a114-4e7a7686bcca",
              "short_code": "tnm",
              "logo": null,
              "supports_withdrawals": true,
              "supported_country": {
                "name": "Malawi",
                "currency": "MWK"
              }
            },
            {
              "id": 2,
              "name": "Airtel Money",
              "ref_id": "20be6c20-adeb-4b5b-a7ba-0769820df4fb",
              "short_code": "airtel",
              "logo": null,
              "supports_withdrawals": true,
              "supported_country": {
                "name": "Malawi",
                "currency": "MWK"
              }
            },
            {
              "id": 5,
              "name": "Changu Wallet",
              "ref_id": "550c35a2-86aa-4590-9931-8f23e664cee9",
              "short_code": "changu_wallet",
              "logo": null,
              "supports_withdrawals": true,
              "supported_country": {
                "name": "Malawi",
                "currency": "MWK"
              }
            }
          ]
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

        var result = await client.GetMobileMoneyOperatorsAsync();

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(responseContent, result.RawJsonResponse);

        Assert.NotNull(result.Result);
        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Supported mobile money operators retrieved successfully.",
            result.Result.Message);

        Assert.Equal(3, result.Result.Data.Count);

        var tnm = result.Result.Data[0];

        Assert.Equal(1, tnm.Id);
        Assert.Equal("TNM Mpamba", tnm.Name);
        Assert.Equal(
            "27494cb5-ba9e-437f-a114-4e7a7686bcca",
            tnm.RefId);
        Assert.Equal("tnm", tnm.ShortCode);
        Assert.Null(tnm.Logo);
        Assert.True(tnm.SupportsWithdrawals);
        Assert.Equal("Malawi", tnm.SupportedCountry.Name);
        Assert.Equal("MWK", tnm.SupportedCountry.Currency);

        var airtel = result.Result.Data[1];

        Assert.Equal(2, airtel.Id);
        Assert.Equal("Airtel Money", airtel.Name);
        Assert.Equal("airtel", airtel.ShortCode);

        var changuWallet = result.Result.Data[2];

        Assert.Equal(5, changuWallet.Id);
        Assert.Equal("Changu Wallet", changuWallet.Name);
        Assert.Equal("changu_wallet", changuWallet.ShortCode);
    }

    [Fact]
    public async Task GetMobileMoneyOperatorsAsync_SendsCorrectHttpRequest()
    {
        const string responseContent = """
        {
          "status": "success",
          "message": "Supported mobile money operators retrieved successfully.",
          "data": []
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

        await client.GetMobileMoneyOperatorsAsync();

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}mobile-money"),
            handler.RequestUri);
    }

    [Fact]
    public async Task GetMobileMoneyOperatorsAsync_ReturnsFailure_WhenRequestFails()
    {
        const string responseContent = """
        {
          "status": "failed",
          "message": "Unable to retrieve supported mobile money operators."
        }
        """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    responseContent,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);

        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var result = await client.GetMobileMoneyOperatorsAsync();

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Result);
        Assert.Equal(responseContent, result.RawJsonResponse);
    }

    [Fact]
    public async Task GetMobileMoneyOperatorsAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        const string responseContent = "Invalid JSON response";

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

        var result = await client.GetMobileMoneyOperatorsAsync();

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);
        Assert.Null(result.Result);
        Assert.Equal(responseContent, result.RawJsonResponse);
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
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Result);

        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Payment request initiated successfully.",
            result.Result.Message);

        Assert.Equal("charge-001", result.Result.Data.ChargeId);
        Assert.Equal("ref-001", result.Result.Data.ReferenceId);
        Assert.Equal("pending", result.Result.Data.Status);
        Assert.Equal(5000m, result.Result.Data.Amount);
        Assert.Equal("Airtel Money", result.Result.Data.MobileMoney.Name);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        const string errorContent = """
            {
              "status": "failed",
              "message": "Session has expired for last transaction. Please try again",
              "data": null
            }
            """;


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
        var result = await client.InitiateMobileMoneyDirectChargeAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Result);

        Assert.Equal(errorContent, result.RawJsonResponse);

        Assert.Equal(
            "Session has expired for last transaction. Please try again",
            result.ErrorMessage);
    }

    [Fact]
    public async Task InitiateMobileMoneyChargeAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string invalidJson = "invalid-json";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson)
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
        var result = await client.InitiateMobileMoneyDirectChargeAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);

        Assert.Equal(invalidJson, result.RawJsonResponse);
        Assert.Null(result.Result);
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
    public async Task VerifyPaymentAsync_MobileMoneyDirectChargeWithMissingData_ReturnsFailure()
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
        var result = await client.VerifyPaymentAsync(
            "test-charge-id",
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.MobileMoney);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(
            "empty Mobile Money Direct Charge verification data payload",
            result.ErrorMessage);

        Assert.Equal(responseContent, result.RawJsonResponse);
        Assert.Null(result.Result);
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
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);

        Assert.Equal(
            PayChanguPaymentFlow.DirectCharge,
            result.Result.PaymentFlow);

        Assert.Equal(
            PayChanguPaymentMethod.BankTransfer,
            result.Result.PaymentMethod);

        Assert.Equal(chargeId, result.Result.TransactionIdentifier);
        Assert.Equal("bank-ref-001", result.Result.TransactionReference);
        Assert.Equal("bank-trans-001", result.Result.TransactionId);

        Assert.Equal("success", result.Result.ApiStatus);
        Assert.Equal("success", result.Result.Status);

        Assert.Equal(10000m, result.Result.Amount);
        Assert.Equal("MWK", result.Result.Currency);

        Assert.NotNull(result.Result.Charges);
        Assert.Equal(200m, result.Result.Charges.Amount);

        Assert.Equal("Test Bank", result.Result.Authorization!.PayerBank);
        Assert.Equal(
            "1234567890",
            result.Result.Authorization.PayerAccountNumber);

        Assert.Equal(
            "Jane Banda",
            result.Result.Authorization.PayerAccountName);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}direct-charge/transactions/{chargeId}/details"),
            handler.RequestUri);
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
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Result);

        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Payment initialized successfully.",
            result.Result.Message);

        Assert.Equal(
            "Centenary Bank",
            result.Result.Data.PaymentAccountDetails.BankName);

        Assert.Equal(
            "2652493369",
            result.Result.Data.PaymentAccountDetails.AccountNumber);

        Assert.Equal(
            "PayChangu Dev",
            result.Result.Data.PaymentAccountDetails.AccountName);

        Assert.Equal(
            1739830475L,
            result.Result.Data.PaymentAccountDetails.AccountExpirationTimestamp);

        Assert.Equal("PTC12383", result.Result.Data.Transaction.ChargeId);
        Assert.Equal("75513659949", result.Result.Data.Transaction.ReferenceId);
        Assert.Equal(1020m, result.Result.Data.Transaction.Amount);
        Assert.Equal("MK", result.Result.Data.Transaction.Currency);
        Assert.Equal("pending", result.Result.Data.Transaction.Status);

        Assert.Equal(
            "Mobile Bank Transfer",
            result.Result.Data.Transaction.Authorization!.Channel);

        Assert.Single(result.Result.Data.Transaction.Logs);

        Assert.Equal(
            "Attempted to pay with bank transfer",
            result.Result.Data.Transaction.Logs[0].Message);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_ReturnsFailure_WhenRequestFails()
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
        var result = await client.InitiateBankTransferDirectChargeAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Result);

        Assert.Equal(errorContent, result.RawJsonResponse);
    }

    [Fact]
    public async Task InitiateBankTransferDirectChargeAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string invalidJson = "invalid-json";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateBankTransferDirectChargeRequest
        {
            Amount = "5000",
            ChargeId = "bank-charge-001"
        };

        // Act
        var result = await client.InitiateBankTransferDirectChargeAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);

        Assert.Equal(invalidJson, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task VerifyPaymentAsync_BankTransferDirectChargeWithMissingTransaction_ReturnsFailure()
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
        var result = await client.VerifyPaymentAsync(
            "test-charge-id",
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.BankTransfer);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(
            "empty Bank Transfer Direct Charge verification transaction payload",
            result.ErrorMessage);

        Assert.Equal(responseContent, result.RawJsonResponse);
        Assert.Null(result.Result);
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
    public async Task VerifyPaymentAsync_DirectChargeCard_ReturnsSuccess_WhenRequestSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.VerifyPaymentAsync(
            "charge-001",
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.Card);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.Result);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
    }

    [Fact]
    public async Task VerifyPaymentAsync_DirectChargeCard_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.VerifyPaymentAsync(
            "charge-001",
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.Card);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Result);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsBalance_WhenRequestSucceeds()
    {
        // Arrange
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

        // Act
        var result = await client.GetBalanceAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Result);

        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Wallet balances retrieved successfully",
            result.Result.Message);

        Assert.Equal("live", result.Result.Data.Environment);
        Assert.Equal("MWK", result.Result.Data.Currency);
        Assert.Equal("953037.95", result.Result.Data.MainBalance);
        Assert.Equal(0m, result.Result.Data.CollectionBalance);
    }

    [Fact]
    public async Task GetBalanceAsync_UsesSpecifiedCurrency()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                {
                    "status": "success",
                    "message": "Success",
                    "data": {
                        "environment": "live",
                        "currency": "USD",
                        "main_balance": "100",
                        "collection_balance": 0
                    }
                }
                """)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetBalanceAsync("USD");

        // Assert
        Assert.Equal(
            "https://api.paychangu.com/wallet-balance?currency=USD",
            handler.RequestUri!.ToString());

        Assert.True(result.Success);
        Assert.Equal("USD", result.Result!.Data.Currency);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        const string errorContent =
            """{"status":"failed","message":"Unauthorized"}""";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorContent)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetBalanceAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Result);

        Assert.Equal(errorContent, result.RawJsonResponse);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string invalidJson = "invalid json";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetBalanceAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);

        Assert.Equal(invalidJson, result.RawJsonResponse);
        Assert.Null(result.Result);
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
}