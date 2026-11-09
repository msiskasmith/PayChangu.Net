using Microsoft.Extensions.DependencyInjection;
using PayChangu.NET.Exceptions;
using PayChangu.NET.Extensions;
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
            Currency = PayChanguCurrency.MWK,
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
                Id = Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
                Response = "test-response"
            }
        };
    }

    private static InitiateCardDirectChargeRequest CreateCardChargeRequest()
    {
        return new InitiateCardDirectChargeRequest
        {
            CardNumber = "4111111111111111",
            Expiry = "12/2030",
            Cvv = "123",
            CardholderName = "John Banda",
            Amount = 5000,
            Currency = PayChanguCurrency.MWK,
            ChargeId = "card-charge-001",
            RedirectUrl = "https://example.com/redirect",
            Email = "john@example.com"
        };
    }

    [Fact]
    public async Task InitiateCheckoutAsync_ShouldReturnPaymentResponse_WhenSuccessful()
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
        var result = await client.InitiateCheckoutAsync(
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
    public async Task InitiateCheckoutAsync_ShouldReturnFailureResponse_WhenRequestFails()
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
        var result = await client.InitiateCheckoutAsync(CreatePaymentRequest());

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(errorContent, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task InitiateCheckoutAsync_ShouldReturnFailureResponse_WhenResponseIsInvalid()
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
        var result = await client.InitiateCheckoutAsync(CreatePaymentRequest());

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
    public async Task InitiateCheckoutAsync_ShouldSendCorrectHttpRequest()
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
        await client.InitiateCheckoutAsync(CreatePaymentRequest());

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
            Amount = 5000m,
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
            Amount = 5000m,
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
            Amount = 5000,
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
    public async Task GetMobileMoneyDirectChargeDetailsAsync_ReturnsTransactionDetails_WhenRequestSucceeds()
    {
        // Arrange
        const string chargeId = "74536";

        const string json = """
    {
        "status": "success",
        "message": "Transaction retrieved successfully.",
        "data": {
            "amount": 50,
            "charge_id": "74536",
            "ref_id": "36013728295",
            "trans_id": "e90ec6c4-18b0-4503-b2df-ee19436b31d6",
            "first_name": null,
            "last_name": null,
            "email": null,
            "type": "Direct API Payment",
            "trace_id": "wS3bV5uZkWEnKPv8r9Cv0ikII4BzuJ2S",
            "status": "success",
            "mobile": "+265992xxxx20",
            "attempts": 1,
            "currency": "MK",
            "mode": "live",
            "created_at": "2025-01-03T22:39:48.000000Z",
            "completed_at": "2025-01-03T22:40:10.000000Z",
            "event_type": "api.charge.payment",
            "mobile_money": {
                "name": "Airtel Money",
                "ref_id": "20be6c20-adeb-4b5b-a7ba-0769820df4fb",
                "country": "Malawi"
            },
            "transaction_charges": {
                "currency": "MK",
                "amount": "1.5"
            },
            "authorization": {
                "channel": "Mobile Money",
                "card_number": null,
                "expiry": null,
                "brand": null,
                "provider": "Airtel Money",
                "mobile_number": null,
                "completed_at": "2025-01-03T22:40:10.000000Z"
            },
            "logs": [
                {
                    "type": "log",
                    "message": "Payment transaction initialized and PIN authorization requested.",
                    "created_at": "2025-01-03T22:39:51.000000Z"
                },
                {
                    "type": "log",
                    "message": "Airtel Money payment transaction confirmed successfully.",
                    "created_at": "2025-01-03T22:40:10.000000Z"
                }
            ]
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

        // Act
        var result = await client.GetSingleMobileMoneyChargeDetailsAsync(chargeId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);

        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Transaction retrieved successfully.",
            result.Result.Message);

        var transaction = result.Result.Data;

        Assert.Equal(50m, transaction.Amount);
        Assert.Equal(chargeId, transaction.ChargeId);
        Assert.Equal("36013728295", transaction.ReferenceId);
        Assert.Equal(
            "e90ec6c4-18b0-4503-b2df-ee19436b31d6",
            transaction.TransactionId);

        Assert.Null(transaction.FirstName);
        Assert.Null(transaction.LastName);
        Assert.Null(transaction.Email);

        Assert.Equal("Direct API Payment", transaction.Type);
        Assert.Equal(
            "wS3bV5uZkWEnKPv8r9Cv0ikII4BzuJ2S",
            transaction.TraceId);

        Assert.Equal("success", transaction.Status);
        Assert.Equal("+265992xxxx20", transaction.Mobile);
        Assert.Equal(1, transaction.Attempts);
        Assert.Equal("MK", transaction.Currency);
        Assert.Equal("live", transaction.Mode);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-03T22:39:48.000000Z"),
            transaction.CreatedAt);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-03T22:40:10.000000Z"),
            transaction.CompletedAt);

        Assert.Equal("api.charge.payment", transaction.EventType);

        Assert.NotNull(transaction.MobileMoney);
        Assert.Equal("Airtel Money", transaction.MobileMoney.Name);
        Assert.Equal(
            "20be6c20-adeb-4b5b-a7ba-0769820df4fb",
            transaction.MobileMoney.ReferenceId);
        Assert.Equal("Malawi", transaction.MobileMoney.Country);

        Assert.NotNull(transaction.TransactionCharges);
        Assert.Equal("MK", transaction.TransactionCharges.Currency);
        Assert.Equal(1.5m, transaction.TransactionCharges.Amount);

        Assert.NotNull(transaction.Authorization);
        Assert.Equal("Mobile Money", transaction.Authorization.Channel);
        Assert.Equal("Airtel Money", transaction.Authorization.Provider);
        Assert.Null(transaction.Authorization.CardNumber);
        Assert.Null(transaction.Authorization.MobileNumber);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-03T22:40:10.000000Z"),
            transaction.Authorization.CompletedAt);

        Assert.Equal(2, transaction.Logs.Count);

        Assert.Equal(
            "Payment transaction initialized and PIN authorization requested.",
            transaction.Logs[0].Message);

        Assert.Equal(
            "Airtel Money payment transaction confirmed successfully.",
            transaction.Logs[1].Message);
    }

    [Fact]
    public async Task GetMobileMoneyDirectChargeDetailsAsync_SendsCorrectHttpRequest()
    {
        // Arrange
        const string chargeId = "74536";

        const string responseContent = """
    {
        "status": "success",
        "message": "Transaction retrieved successfully.",
        "data": {}
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
        await client.GetSingleMobileMoneyChargeDetailsAsync(chargeId);

        // Assert
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);

        Assert.Equal(
            new Uri($"{BaseUrl}mobile-money/payments/{chargeId}/details"),
            handler.RequestUri);

        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task GetMobileMoneyDirectChargeDetailsAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        const string errorContent = """
    {
        "status": "failed",
        "message": "Transaction not found.",
        "data": null
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    errorContent,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetSingleMobileMoneyChargeDetailsAsync("74536");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

        Assert.Equal("Transaction not found.", result.ErrorMessage);

        Assert.Null(result.Result);
        Assert.Equal(errorContent, result.RawJsonResponse);
    }

    [Fact]
    public async Task GetMobileMoneyDirectChargeDetailsAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string invalidJson = "invalid-json";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    invalidJson,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetSingleMobileMoneyChargeDetailsAsync("74536");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);

        Assert.Null(result.Result);
        Assert.Equal(invalidJson, result.RawJsonResponse);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMobileMoneyDirectChargeDetailsAsync_WhenChargeIdIsInvalid_ThrowsPayChanguException(
    string? chargeId)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<PayChanguException>(
            () => client.GetSingleMobileMoneyChargeDetailsAsync(chargeId!));

        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenTransactionChargeAmountIsInvalid_ReturnsZeroAmount()
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
        var response = await client.VerifyPaymentAsync(
            "charge-001",
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.MobileMoney);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Result);
        Assert.NotNull(response.RawJsonResponse);

        Assert.Equal(5000m, response.Result.Amount);
        Assert.Equal(0m, response.Result.Charges?.Amount);
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
            Amount = 1020,
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
            Amount = 5000m,
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
            Amount = 5000,
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
    public async Task GetBankTransferDirectChargeDetailsAsync_ReturnsTransactionDetails_WhenRequestSucceeds()
    {
        // Arrange
        const string responseJson = """
    {
      "status": "success",
      "message": "Transaction retrieved successfully.",
      "data": {
        "transaction": {
          "charge_id": "PTC12383",
          "ref_id": "75513659949",
          "trans_id": null,
          "currency": "MK",
          "amount": 1000,
          "first_name": null,
          "last_name": null,
          "email": null,
          "type": "Direct API Payment",
          "trace_id": "v0atWHs0NLEN3vbTAL4DKFLm59dVHuRM",
          "status": "success",
          "mobile": "0",
          "attempts": 1,
          "mode": "live",
          "created_at": "2025-02-17T21:14:35.000000Z",
          "completed_at": "2025-02-17T21:18:40.000000Z",
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
            "payer_bank_uuid": "82310dd1-ec9b-4fe7-a32c-2f262ef08681",
            "payer_bank": "National Bank of Malawi",
            "payer_account_number": "1007134421",
            "payer_account_name": "Jonathan Manda",
            "completed_at": "2025-02-17T21:18:40.000000Z"
          },
          "logs": [
            {
              "type": "log",
              "message": "Attempted to pay with bank transfer",
              "created_at": "2025-02-17T21:14:35.000000Z"
            },
            {
              "type": "log",
              "message": "Bank transfer payment processed successfully.",
              "created_at": "2025-02-17T21:18:40.000000Z"
            }
          ]
        }
      }
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            });

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        var response = await client.GetSingleBankDirectChargeDetailsAsync(
            "PTC12383");

        // Assert
        Assert.True(response.Success);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.ErrorMessage);
        Assert.NotNull(response.Result);
        Assert.Equal(responseJson, response.RawJsonResponse);

        var result = response.Result;

        Assert.Equal("success", result.Status);
        Assert.Equal("Transaction retrieved successfully.", result.Message);

        var transaction = result.Data.Transaction;

        Assert.Equal("PTC12383", transaction.ChargeId);
        Assert.Equal("75513659949", transaction.ReferenceId);
        Assert.Null(transaction.TransactionId);
        Assert.Equal("MK", transaction.Currency);
        Assert.Equal(1000m, transaction.Amount);
        Assert.Equal("Direct API Payment", transaction.Type);
        Assert.Equal("v0atWHs0NLEN3vbTAL4DKFLm59dVHuRM", transaction.TraceId);
        Assert.Equal("success", transaction.Status);
        Assert.Equal("0", transaction.Mobile);
        Assert.Equal(1, transaction.Attempts);
        Assert.Equal("live", transaction.Mode);
        Assert.Equal("api.charge.payment", transaction.EventType);

        Assert.NotNull(transaction.TransactionCharges);
        Assert.Equal("MK", transaction.TransactionCharges.Currency);
        Assert.Equal(20m, transaction.TransactionCharges.Amount);

        Assert.NotNull(transaction.Authorization);
        Assert.Equal("Mobile Bank Transfer", transaction.Authorization.Channel);
        Assert.Equal(
            Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
            transaction.Authorization.PayerBankId);
        Assert.Equal(
            "National Bank of Malawi",
            transaction.Authorization.PayerBank);
        Assert.Equal(
            "1007134421",
            transaction.Authorization.PayerAccountNumber);
        Assert.Equal(
            "Jonathan Manda",
            transaction.Authorization.PayerAccountName);

        Assert.NotNull(transaction.Logs);
        Assert.Equal(2, transaction.Logs.Count);
        Assert.Equal(
            "Attempted to pay with bank transfer",
            transaction.Logs[0].Message);
        Assert.Equal(
            "Bank transfer payment processed successfully.",
            transaction.Logs[1].Message);
    }

    [Fact]
    public async Task GetBankTransferDirectChargeDetailsAsync_SendsCorrectHttpRequest()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                {
                    "status": "success",
                    "message": "Transaction retrieved successfully.",
                    "data": {
                        "transaction": {}
                    }
                }
                """)
            });

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.GetSingleBankDirectChargeDetailsAsync("PTC12383");

        // Assert
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            $"{BaseUrl}direct-charge/transactions/PTC12383/details",
            handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task GetBankTransferDirectChargeDetailsAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        const string responseJson = """
    {
        "status": "failed",
        "message": "Transaction not found.",
        "data": null
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(responseJson)
            });

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        var response = await client.GetSingleBankDirectChargeDetailsAsync(
            "PTC12383");

        // Assert
        Assert.False(response.Success);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Transaction not found.", response.ErrorMessage);
        Assert.Null(response.Result);
        Assert.Equal(responseJson, response.RawJsonResponse);
    }

    [Fact]
    public async Task GetBankTransferDirectChargeDetailsAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string invalidJson = "{ invalid json }";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson)
            });

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        var response = await client.GetSingleBankDirectChargeDetailsAsync(
            "PTC12383");

        // Assert
        Assert.False(response.Success);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Unable to deserialize", response.ErrorMessage);
        Assert.Null(response.Result);
        Assert.Equal(invalidJson, response.RawJsonResponse);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task GetBankTransferDirectChargeDetailsAsync_ThrowsPayChanguException_WhenChargeIdIsInvalid(
    string? chargeId)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.GetSingleBankDirectChargeDetailsAsync(chargeId!));

        // Assert
        Assert.Equal(
            "The charge ID cannot be null, empty, or whitespace.",
            exception.Message);

        Assert.Null(handler.RequestUri);
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
            Amount = 5000m,
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
            Amount = 5000m,
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
            Amount = 5000m,
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
            Amount = 5000m,
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
            Amount = 5000m,
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
            Amount = 5000m,
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
    public async Task InitiateCardChargeAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        const string json = """
    {
        "success": true,
        "requires_3ds_auth": true,
        "orderReference": "00b1b3d0-1821-48ec-8136-4904ded5923b",
        "3ds_auth_link": "https://in.paychangu.com/3ds/auth?reference=5984ae57-9920-4c30-abe5-82e07cb14234"
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

        // Act
        var result = await client.InitiateCardChargeAsync(
            CreateCardChargeRequest());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);

        Assert.True(result.Result.Success);
        Assert.True(result.Result.Requires3DSAuth);

        Assert.Equal(
            "00b1b3d0-1821-48ec-8136-4904ded5923b",
            result.Result.OrderReference);

        Assert.Equal(
            "https://in.paychangu.com/3ds/auth?reference=5984ae57-9920-4c30-abe5-82e07cb14234",
            result.Result.ThreeDSAuthLink);
    }

    [Fact]
    public async Task InitiateCardChargeAsync_UsesCorrectEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                {
                    "success": true,
                    "requires_3ds_auth": false,
                    "orderReference": "order-001",
                    "3ds_auth_link": null
                }
                """,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.InitiateCardChargeAsync(CreateCardChargeRequest());

        // Assert
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);

        Assert.Equal(
            new Uri($"{BaseUrl}charge-card/payments"),
            handler.RequestUri);
    }

    [Fact]
    public async Task InitiateCardChargeAsync_SerializesRequestCorrectly()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                {
                    "success": true,
                    "requires_3ds_auth": false,
                    "orderReference": "order-001",
                    "3ds_auth_link": null
                }
                """)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.InitiateCardChargeAsync(CreateCardChargeRequest());

        // Assert
        Assert.NotNull(handler.RequestContent);

        using var document = JsonDocument.Parse(handler.RequestContent);
        var root = document.RootElement;

        Assert.Equal(
            "4111111111111111",
            root.GetProperty("card_number").GetString());

        Assert.Equal("12/2030", root.GetProperty("expiry").GetString());
        Assert.Equal("123", root.GetProperty("cvv").GetString());

        Assert.Equal(
            "John Banda",
            root.GetProperty("cardholder_name").GetString());

        Assert.Equal("5000", root.GetProperty("amount").GetString());
        Assert.Equal("MWK", root.GetProperty("currency").GetString());
        Assert.Equal("card-charge-001", root.GetProperty("charge_id").GetString());

        Assert.Equal(
            "https://example.com/redirect",
            root.GetProperty("redirect_url").GetString());

        Assert.Equal(
            "john@example.com",
            root.GetProperty("email").GetString());
    }

    [Fact]
    public async Task InitiateCardChargeAsync_UsesBearerAuthentication()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                {
                    "success": true,
                    "requires_3ds_auth": false,
                    "orderReference": "order-001",
                    "3ds_auth_link": null
                }
                """)
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.InitiateCardChargeAsync(CreateCardChargeRequest());

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task InitiateCardChargeAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        const string errorContent = """
    {
        "message": "Invalid card details",
        "status": "failed"
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    errorContent,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.InitiateCardChargeAsync(
            CreateCardChargeRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Invalid card details", result.ErrorMessage);

        Assert.Null(result.Result);
        Assert.Equal(errorContent, result.RawJsonResponse);
    }

    [Fact]
    public async Task InitiateCardChargeAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.InitiateCardChargeAsync(null!));

        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task RefundCardChargeAsync_WhenSuccessful_ReturnsSuccessResponse()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        var response = await client.RefundCardChargeAsync("card-charge-001");

        // Assert
        Assert.True(response.Success);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task RefundCardChargeAsync_SendsPostRequestToCorrectEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.RefundCardChargeAsync("card-charge-001");

        // Assert
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}charge-card/refund/card-charge-001"),
            handler.RequestUri);
    }

    [Fact]
    public async Task RefundCardChargeAsync_DoesNotSendRequestBody()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.RefundCardChargeAsync("card-charge-001");

        // Assert
        Assert.Null(handler.RequestContent);
    }

    [Fact]
    public async Task RefundCardChargeAsync_SendsBearerAuthentication()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.RefundCardChargeAsync("card-charge-001");

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task RefundCardChargeAsync_WhenHttpRequestFails_ReturnsFailureResponse()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        var response = await client.RefundCardChargeAsync("card-charge-001");

        // Assert
        Assert.False(response.Success);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Card Direct Charge refund failed with HTTP status code 400.",
            response.ErrorMessage);
    }

    [Fact]
    public async Task RefundCardChargeAsync_WhenChargeIdContainsSpecialCharacters_EscapesChargeId()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        const string chargeId = "card/charge 001";

        // Act
        await client.RefundCardChargeAsync(chargeId);

        // Assert
        Assert.Equal(
            new Uri($"{BaseUrl}charge-card/refund/card%2Fcharge%20001"),
            handler.RequestUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task RefundCardChargeAsync_WhenChargeIdIsInvalid_ThrowsPayChanguException(
    string? chargeId)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var provider = CreateServiceProvider(handler);
        var client = provider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.RefundCardChargeAsync(chargeId!));

        // Assert
        Assert.Equal(
            "The charge ID cannot be null, empty, or whitespace.",
            exception.Message);

        Assert.Null(handler.RequestUri);
    }


    [Fact]
    public async Task VerifyPaymentAsync_DirectChargeCard_ReturnsNormalizedResponseAndRawJson()
    {
        // Arrange
        const string chargeId = "charge-001";

        const string json = """
    {
        "status": "success",
        "message": "Payment details retrieved successfully.",
        "data": {
            "event_type": "api.charge",
            "tx_ref": "charge-001",
            "mode": "live",
            "type": "Direct API Payment (Card)",
            "status": "success",
            "number_of_attempts": 1,
            "reference": "card-ref-001",
            "currency": "MWK",
            "amount": 5000,
            "charges": 100,
            "customization": null,
            "meta": null,
            "authorization": {
                "channel": "Card",
                "card_number": "411111******1111",
                "expiry": "2030-12",
                "brand": "VISA",
                "provider": null,
                "mobile_number": null,
                "completed_at": "2026-11-05T10:01:00Z"
            },
            "customer": {
                "email": "john@example.com",
                "first_name": "John",
                "last_name": "Banda"
            },
            "logs": [],
            "created_at": "2026-11-05T10:00:00Z",
            "updated_at": "2026-11-05T10:01:00Z"
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

        // Act
        var result = await client.VerifyPaymentAsync(
            chargeId,
            PayChanguPaymentFlow.DirectCharge,
            PayChanguPaymentMethod.Card);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);

        Assert.Equal(
            PayChanguPaymentFlow.DirectCharge,
            result.Result.PaymentFlow);

        Assert.Equal(
            PayChanguPaymentMethod.Card,
            result.Result.PaymentMethod);

        Assert.Equal(chargeId, result.Result.TransactionIdentifier);
        Assert.Equal(chargeId, result.Result.TransactionReference);
        Assert.Equal("card-ref-001", result.Result.ProviderReference);
        Assert.Equal("api.charge", result.Result.EventType);

        Assert.Equal("success", result.Result.ApiStatus);
        Assert.Equal("success", result.Result.Status);

        Assert.Equal(1, result.Result.Attempts);
        Assert.Equal(5000m, result.Result.Amount);
        Assert.Equal("MWK", result.Result.Currency);

        Assert.NotNull(result.Result.Charges);
        Assert.Equal("MWK", result.Result.Charges.Currency);
        Assert.Equal(100m, result.Result.Charges.Amount);

        Assert.Equal("VISA", result.Result.Authorization!.Brand);
        Assert.Equal("411111******1111", result.Result.Authorization.CardNumber);
        Assert.Equal("john@example.com", result.Result.Customer!.Email);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}charge-card/verify/{chargeId}"),
            handler.RequestUri);
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
    public async Task InitiateMobileMoneyPayoutAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        const string json = """
    {
        "status": "success",
        "message": "Payout processed successfully.",
        "data": {
            "amount": 610.2,
            "charge_id": "6789",
            "ref_id": "86662145330",
            "trans_id": null,
            "first_name": null,
            "last_name": null,
            "email": null,
            "type": "API Payout",
            "trace_id": null,
            "status": "success",
            "mobile": "+265992xxxx20",
            "attempts": 1,
            "currency": "MK",
            "mode": "sandbox",
            "created_at": "2025-01-06T18:25:37.000000Z",
            "completed_at": "2025-01-06T18:25:37.000000Z",
            "event_type": "api.payout",
            "mobile_money": {
                "name": "Airtel Money",
                "ref_id": "20be6c20-adeb-4b5b-a7ba-0769820df4fb",
                "country": "Malawi"
            },
            "transaction_charges": {
                "currency": "MK",
                "amount": 10.2
            },
            "authorization": {
                "channel": "Mobile Money",
                "card_number": null,
                "expiry": null,
                "brand": null,
                "provider": "Airtel Money",
                "mobile_number": null,
                "completed_at": "2025-01-06T18:25:37.000000Z"
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

        var request = new InitiateMobileMoneyPayoutRequest
        {
            Mobile = "+265992xxxx20",
            MobileMoneyOperatorRefId =
                "20be6c20-adeb-4b5b-a7ba-0769820df4fb",
            Amount = 610.2m,
            ChargeId = "6789"
        };

        // Act
        var result = await client.InitiateMobileMoneyPayoutAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);
        Assert.Equal("success", result.Result.Status);
        Assert.Equal("Payout processed successfully.", result.Result.Message);

        var transaction = result.Result.Data;

        Assert.Equal(610.2m, transaction.Amount);
        Assert.Equal("6789", transaction.ChargeId);
        Assert.Equal("86662145330", transaction.ReferenceId);
        Assert.Null(transaction.TransactionId);

        Assert.Null(transaction.FirstName);
        Assert.Null(transaction.LastName);
        Assert.Null(transaction.Email);

        Assert.Equal("API Payout", transaction.Type);
        Assert.Null(transaction.TraceId);
        Assert.Equal("success", transaction.Status);
        Assert.Equal("+265992xxxx20", transaction.Mobile);
        Assert.Equal(1, transaction.Attempts);
        Assert.Equal("MK", transaction.Currency);
        Assert.Equal("sandbox", transaction.Mode);
        Assert.Equal("api.payout", transaction.EventType);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-06T18:25:37.000000Z"),
            transaction.CreatedAt);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-06T18:25:37.000000Z"),
            transaction.CompletedAt);

        Assert.NotNull(transaction.MobileMoney);
        Assert.Equal("Airtel Money", transaction.MobileMoney.Name);
        Assert.Equal(
            "20be6c20-adeb-4b5b-a7ba-0769820df4fb",
            transaction.MobileMoney.ReferenceId);
        Assert.Equal("Malawi", transaction.MobileMoney.Country);

        Assert.NotNull(transaction.TransactionCharges);
        Assert.Equal("MK", transaction.TransactionCharges.Currency);
        Assert.Equal(10.2m, transaction.TransactionCharges.Amount);

        Assert.NotNull(transaction.Authorization);
        Assert.Equal("Mobile Money", transaction.Authorization.Channel);
        Assert.Equal("Airtel Money", transaction.Authorization.Provider);
        Assert.Null(transaction.Authorization.CardNumber);
        Assert.Null(transaction.Authorization.MobileNumber);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-06T18:25:37.000000Z"),
            transaction.Authorization.CompletedAt);
    }

    [Fact]
    public async Task InitiateMobileMoneyPayoutAsync_UsesCorrectEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Payout processed successfully.","data":{}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyPayoutRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = 5000m,
            ChargeId = "payout-001"
        };

        // Act
        await client.InitiateMobileMoneyPayoutAsync(request);

        // Assert
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}mobile-money/payouts/initialize"),
            handler.RequestUri);
    }

    [Fact]
    public async Task InitiateMobileMoneyPayoutAsync_SerializesRequestCorrectly()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Payout processed successfully.","data":{}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyPayoutRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = 5000m,
            ChargeId = "payout-001",
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Banda",
            TransactionStatus = "successful"
        };

        // Act
        await client.InitiateMobileMoneyPayoutAsync(request);

        // Assert
        Assert.NotNull(handler.RequestContent);

        using var document = JsonDocument.Parse(handler.RequestContent);
        var root = document.RootElement;

        Assert.Equal(
            "+265991234567",
            root.GetProperty("mobile").GetString());

        Assert.Equal(
            "operator-001",
            root.GetProperty("mobile_money_operator_ref_id").GetString());

        Assert.Equal("5000", root.GetProperty("amount").GetString());
        Assert.Equal("payout-001", root.GetProperty("charge_id").GetString());
        Assert.Equal("john@example.com", root.GetProperty("email").GetString());
        Assert.Equal("John", root.GetProperty("first_name").GetString());
        Assert.Equal("Banda", root.GetProperty("last_name").GetString());

        Assert.Equal(
            "successful",
            root.GetProperty("transaction_status").GetString());
    }

    [Fact]
    public async Task InitiateMobileMoneyPayoutAsync_UsesBearerAuthentication()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"success","message":"Payout processed successfully.","data":{}}""")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyPayoutRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = 5000m,
            ChargeId = "payout-001"
        };

        // Act
        await client.InitiateMobileMoneyPayoutAsync(request);

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task InitiateMobileMoneyPayoutAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        const string errorContent = """
    {
        "status": "failed",
        "message": "Unable to process payout.",
        "data": null
    }
    """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    errorContent,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyPayoutRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = 5000m,
            ChargeId = "payout-001"
        };

        // Act
        var result = await client.InitiateMobileMoneyPayoutAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Unable to process payout.", result.ErrorMessage);

        Assert.Null(result.Result);
        Assert.Equal(errorContent, result.RawJsonResponse);
    }

    [Fact]
    public async Task InitiateMobileMoneyPayoutAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string invalidJson = "invalid-json";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    invalidJson,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        var request = new InitiateMobileMoneyPayoutRequest
        {
            Mobile = "+265991234567",
            MobileMoneyOperatorRefId = "operator-001",
            Amount = 5000m,
            ChargeId = "payout-001"
        };

        // Act
        var result = await client.InitiateMobileMoneyPayoutAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unable to deserialize", result.ErrorMessage);

        Assert.Equal(invalidJson, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task InitiateMobileMoneyPayoutAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.InitiateMobileMoneyPayoutAsync(null!));

        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task InitiateBankPayoutAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        const string json = """
    {
        "status": "success",
        "message": "Payout request submitted successfully.",
        "data": {
            "transaction": {
                "charge_id": "TRX683932Ygis898jjnyghvtgu",
                "ref_id": "97073812876",
                "trans_id": null,
                "currency": "MK",
                "amount": 3000,
                "first_name": null,
                "last_name": null,
                "email": null,
                "type": "API Payout",
                "trace_id": null,
                "status": "pending",
                "mobile": "0",
                "attempts": 1,
                "mode": "live",
                "created_at": "2025-01-23T17:03:28.000000Z",
                "completed_at": null,
                "event_type": "api.payout",
                "transaction_charges": {
                    "currency": "MK",
                    "amount": "0"
                },
                "recipient_account_details": {
                    "bank_uuid": "82310dd1-ec9b-4fe7-a32c-2f262ef08681",
                    "bank_name": "National Bank of Malawi",
                    "account_name": "Madalitso Kamwendo",
                    "account_number": "1000000010"
                }
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

        var request = new InitiateBankPayoutRequest
        {
            BankId = Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
            Amount = 3000m,
            ChargeId = "TRX683932Ygis898jjnyghvtgu",
            BankAccountName = "Madalitso Kamwendo",
            BankAccountNumber = "1000000010"
        };

        // Act
        var result = await client.InitiateBankPayoutAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);
        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Payout request submitted successfully.",
            result.Result.Message);

        Assert.NotNull(result.Result.Data);

        var transaction = result.Result.Data.Transaction;

        Assert.Equal("TRX683932Ygis898jjnyghvtgu", transaction.ChargeId);
        Assert.Equal("97073812876", transaction.ReferenceId);
        Assert.Null(transaction.TransactionId);

        Assert.Equal("MK", transaction.Currency);
        Assert.Equal(3000m, transaction.Amount);

        Assert.Null(transaction.FirstName);
        Assert.Null(transaction.LastName);
        Assert.Null(transaction.Email);

        Assert.Equal("API Payout", transaction.Type);
        Assert.Null(transaction.TraceId);
        Assert.Equal("pending", transaction.Status);
        Assert.Equal("0", transaction.Mobile);
        Assert.Equal(1, transaction.Attempts);
        Assert.Equal("live", transaction.Mode);
        Assert.Equal("api.payout", transaction.EventType);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-23T17:03:28.000000Z"),
            transaction.CreatedAt);

        Assert.Null(transaction.CompletedAt);

        Assert.NotNull(transaction.TransactionCharges);
        Assert.Equal("MK", transaction.TransactionCharges.Currency);
        Assert.Equal(0m, transaction.TransactionCharges.Amount);

        Assert.NotNull(transaction.RecipientAccountDetails);
        Assert.Equal(
            Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
            transaction.RecipientAccountDetails.BankId);
        Assert.Equal(
            "National Bank of Malawi",
            transaction.RecipientAccountDetails.BankName);
        Assert.Equal(
            "Madalitso Kamwendo",
            transaction.RecipientAccountDetails.AccountName);
        Assert.Equal(
            "1000000010",
            transaction.RecipientAccountDetails.AccountNumber);
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
        var result = await client.GetBalanceAsync(PayChanguCurrency.USD);

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

    [Fact]
    public async Task GetSupportedBanksAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        const string json = """
{
    "status": "success",
    "message": "Retrieved successfully.",
    "data": [
        {
            "uuid": "82310dd1-ec9b-4fe7-a32c-2f262ef08681",
            "name": "National Bank of Malawi"
        },
        {
            "uuid": "87e62436-0553-4fb5-a76d-f27d28420c5b",
            "name": "Ecobank Malawi Limited"
        },
        {
            "uuid": "b064172a-8a1b-4f7f-aad7-81b036c46c57",
            "name": "FDH Bank Limited"
        },
        {
            "uuid": "e7447c2c-c147-4907-b194-e087fe8d8585",
            "name": "Standard Bank Limited"
        },
        {
            "uuid": "236760c9-3045-4a01-990e-497b28d115bb",
            "name": "Centenary Bank"
        },
        {
            "uuid": "968ac588-3b1f-4d89-81ff-a3d43a599003",
            "name": "First Capital Limited"
        },
        {
            "uuid": "c759d7b6-ae5c-4a95-814a-79171271897a",
            "name": "CDH Investment Bank"
        },
        {
            "uuid": "5e9946ae-76ed-43f5-ad59-63e09096006a",
            "name": "TNM Mpamba"
        },
        {
            "uuid": "e8d5fca0-e5ac-4714-a518-484be9011326",
            "name": "Airtel Money"
        },
        {
            "uuid": "86007bf5-1b04-49ba-84c1-9758bbf5c996",
            "name": "NBS Bank Limited"
        }
    ]
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
        var response = await client.GetSupportedBanksAsync(PayChanguCurrency.MWK);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Result);
        Assert.NotNull(response.RawJsonResponse);

        Assert.Equal("success", response.Result.Status);
        Assert.Equal("Retrieved successfully.", response.Result.Message);

        Assert.NotNull(response.Result.Data);
        Assert.Equal(10, response.Result.Data.Count);

        Assert.Equal(
            Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
            response.Result.Data[0].Id);

        Assert.Equal(
            "National Bank of Malawi",
            response.Result.Data[0].Name);

        Assert.Equal("TNM Mpamba", response.Result.Data[7].Name);
        Assert.Equal("Airtel Money", response.Result.Data[8].Name);
        Assert.Equal("NBS Bank Limited", response.Result.Data[9].Name);
    }

    [Fact]
    public async Task GetSupportedBanksAsync_UsesCorrectEndpoint()
    {
        // Arrange
        const string json = """
{
    "status": "success",
    "message": "Retrieved successfully.",
    "data": []
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
        await client.GetSupportedBanksAsync(PayChanguCurrency.MWK);

        // Assert
        Assert.Equal(
            "https://api.paychangu.com/direct-charge/payouts/supported-banks?currency=MWK",
            handler.RequestUri?.ToString());

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
    }

    [Fact]
    public async Task GetSupportedBanksAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        // Arrange
        const string json = "{ invalid json }";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        using var serviceProvider = CreateServiceProvider(handler);

        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var response = await client.GetSupportedBanksAsync(PayChanguCurrency.MWK);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("Unable to deserialize", response.ErrorMessage);
        Assert.Null(response.Result);
        Assert.Equal(json, response.RawJsonResponse);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task GetSupportedBanksAsync_ThrowsArgumentException_WhenCurrencyIsEmpty(
    string currency)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        using var serviceProvider = CreateServiceProvider(handler);

        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => client.GetSupportedBanksAsync(PayChanguCurrency.MWK));

        // Assert
        Assert.Equal("currency", exception.ParamName);
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task GetBankPayoutDetailsAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        const string json = """
    {
        "status": "successful",
        "message": "Transaction details retrieved successfully",
        "data": {
            "charge_id": "TRX683932Ygis898jjnyghvtguuu",
            "ref_id": "33722634970",
            "trans_id": null,
            "currency": "MK",
            "amount": 1000,
            "first_name": null,
            "last_name": null,
            "email": null,
            "type": "API Payout",
            "trace_id": null,
            "status": "failed",
            "mobile": "0",
            "attempts": 1,
            "mode": "live",
            "created_at": "2025-01-23T22:46:06.000000Z",
            "completed_at": null,
            "event_type": "api.payout",
            "transaction_charges": {
                "currency": "MK",
                "amount": "0"
            },
            "recipient_account_details": {
                "bank_uuid": "82310dd1-ec9b-4fe7-a32c-2f262ef08681",
                "bank_name": "National Bank of Malawi",
                "account_name": "Madalitso Kamwendo",
                "account_number": "1000000010"
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

        const string chargeId = "TRX683932Ygis898jjnyghvtguuu";

        // Act
        var result = await client.GetSingleBankPayoutDetailsAsync(chargeId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);
        Assert.Equal("successful", result.Result.Status);
        Assert.Equal(
            "Transaction details retrieved successfully",
            result.Result.Message);

        var transaction = result.Result.Data;

        Assert.Equal(chargeId, transaction.ChargeId);
        Assert.Equal("33722634970", transaction.ReferenceId);
        Assert.Null(transaction.TransactionId);

        Assert.Equal("MK", transaction.Currency);
        Assert.Equal(1000m, transaction.Amount);

        Assert.Null(transaction.FirstName);
        Assert.Null(transaction.LastName);
        Assert.Null(transaction.Email);

        Assert.Equal("API Payout", transaction.Type);
        Assert.Null(transaction.TraceId);
        Assert.Equal("failed", transaction.Status);
        Assert.Equal("0", transaction.Mobile);
        Assert.Equal(1, transaction.Attempts);
        Assert.Equal("live", transaction.Mode);
        Assert.Equal("api.payout", transaction.EventType);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-23T22:46:06.000000Z"),
            transaction.CreatedAt);

        Assert.Null(transaction.CompletedAt);

        Assert.NotNull(transaction.TransactionCharges);
        Assert.Equal("MK", transaction.TransactionCharges.Currency);
        Assert.Equal(0m, transaction.TransactionCharges.Amount);

        Assert.NotNull(transaction.RecipientAccountDetails);
        Assert.Equal(
            Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
            transaction.RecipientAccountDetails.BankId);
        Assert.Equal(
            "National Bank of Malawi",
            transaction.RecipientAccountDetails.BankName);
        Assert.Equal(
            "Madalitso Kamwendo",
            transaction.RecipientAccountDetails.AccountName);
        Assert.Equal(
            "1000000010",
            transaction.RecipientAccountDetails.AccountNumber);
    }

    [Fact]
    public async Task GetBankPayoutDetailsAsync_UsesCorrectEndpoint()
    {
        // Arrange
        const string chargeId = "TRX683932Ygis898jjnyghvtguuu";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"successful","message":"Success","data":{}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.GetSingleBankPayoutDetailsAsync(chargeId);

        // Assert
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}direct-charge/payouts/{chargeId}/details"),
            handler.RequestUri);
    }

    [Fact]
    public async Task GetBankPayoutDetailsAsync_WhenChargeIdContainsSpecialCharacters_EscapesChargeId()
    {
        // Arrange
        const string chargeId = "TRX/charge 001";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"successful","message":"Success","data":{}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.GetSingleBankPayoutDetailsAsync(chargeId);

        // Assert
        Assert.Equal(
            new Uri($"{BaseUrl}direct-charge/payouts/TRX%2Fcharge%20001/details"),
            handler.RequestUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task GetBankPayoutDetailsAsync_WhenChargeIdIsInvalid_ThrowsPayChanguException(
    string? chargeId)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var exception = await Assert.ThrowsAsync<PayChanguException>(
            () => client.GetSingleBankPayoutDetailsAsync(chargeId!));

        // Assert
        Assert.NotNull(exception);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task GetBankPayoutDetailsAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        const string json = """{"message":"Transaction not found"}""";

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetSingleBankPayoutDetailsAsync("TRX123456");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("Transaction not found", result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetBankPayoutDetailsAsync_ReturnsFailure_WhenResponseContainsInvalidJson()
    {
        // Arrange
        const string json = """{"status":"successful","data":""";

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

        // Act
        var result = await client.GetSingleBankPayoutDetailsAsync("TRX123456");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetBankPayoutDetailsAsync_UsesBearerAuthentication()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"successful","message":"Success","data":{}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        await client.GetSingleBankPayoutDetailsAsync("TRX123456");

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task GetAllBankPayoutDetailsAsync_ReturnsResponse_WhenRequestSucceeds()
    {
        // Arrange
        var json = """
    {
        "status": "success",
        "message": "Transactions retrieved successfully.",
        "data": {
            "current_page": 1,
            "total_pages": 1,
            "per_page": 20,
            "next_page_url": null,
            "data": [
                {
                    "charge_id": "4567tfuty",
                    "ref_id": "54438943842",
                    "trans_id": null,
                    "currency": "MK",
                    "amount": 1000,
                    "first_name": null,
                    "last_name": null,
                    "email": null,
                    "type": "API Payout",
                    "trace_id": null,
                    "status": "success",
                    "mobile": "0",
                    "attempts": 1,
                    "mode": "live",
                    "created_at": "2025-01-24T06:46:40.000000Z",
                    "completed_at": "2025-01-24T06:47:01.000000Z",
                    "event_type": "api.payout",
                    "transaction_charges": {
                        "currency": "MK",
                        "amount": "0"
                    },
                    "recipient_account_details": {
                        "bank_uuid": "82310dd1-ec9b-4fe7-a32c-2f262ef08681",
                        "bank_name": "National Bank of Malawi",
                        "account_name": "Madalitso Kamwendo",
                        "account_number": "1001000010"
                    }
                }
            ]
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
        var result = await client.GetAllBankPayoutDetailsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);

        Assert.NotNull(result.Result);
        Assert.Equal("success", result.Result.Status);
        Assert.Equal(
            "Transactions retrieved successfully.",
            result.Result.Message);

        var pagination = result.Result.Data;

        Assert.Equal(1, pagination.CurrentPage);
        Assert.Equal(1, pagination.TotalPages);
        Assert.Equal(20, pagination.PerPage);
        Assert.Null(pagination.NextPageUrl);

        Assert.Single(pagination.Transactions);

        var transaction = pagination.Transactions[0];

        Assert.Equal("4567tfuty", transaction.ChargeId);
        Assert.Equal("54438943842", transaction.ReferenceId);
        Assert.Null(transaction.TransactionId);

        Assert.Equal("MK", transaction.Currency);
        Assert.Equal(1000m, transaction.Amount);

        Assert.Null(transaction.FirstName);
        Assert.Null(transaction.LastName);
        Assert.Null(transaction.Email);

        Assert.Equal("API Payout", transaction.Type);
        Assert.Null(transaction.TraceId);
        Assert.Equal("success", transaction.Status);
        Assert.Equal("0", transaction.Mobile);
        Assert.Equal(1, transaction.Attempts);
        Assert.Equal("live", transaction.Mode);
        Assert.Equal("api.payout", transaction.EventType);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-24T06:46:40.000000Z"),
            transaction.CreatedAt);

        Assert.Equal(
            DateTimeOffset.Parse("2025-01-24T06:47:01.000000Z"),
            transaction.CompletedAt);

        Assert.NotNull(transaction.TransactionCharges);
        Assert.Equal("MK", transaction.TransactionCharges.Currency);
        Assert.Equal(0m, transaction.TransactionCharges.Amount);

        Assert.NotNull(transaction.RecipientAccountDetails);
        Assert.Equal(
            Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
            transaction.RecipientAccountDetails.BankId);
        Assert.Equal(
            "National Bank of Malawi",
            transaction.RecipientAccountDetails.BankName);
        Assert.Equal(
            "Madalitso Kamwendo",
            transaction.RecipientAccountDetails.AccountName);
        Assert.Equal(
            "1001000010",
            transaction.RecipientAccountDetails.AccountNumber);
    }

    [Fact]
    public async Task GetAllBankPayoutDetailsAsync_UsesCorrectEndpoint()
    {
        // Arrange
        var json = """
    {
        "status": "success",
        "message": "Transactions retrieved successfully.",
        "data": {
            "current_page": 1,
            "total_pages": 1,
            "per_page": 20,
            "next_page_url": null,
            "data": []
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
        await client.GetAllBankPayoutDetailsAsync();

        // Assert
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            new Uri($"{BaseUrl}direct-charge/payouts"),
            handler.RequestUri);
    }

    [Fact]
    public async Task GetAllBankPayoutDetailsAsync_UsesBearerAuthentication()
    {
        // Arrange
        var json = """
    {
        "status": "success",
        "message": "Transactions retrieved successfully.",
        "data": {
            "current_page": 1,
            "total_pages": 1,
            "per_page": 20,
            "next_page_url": null,
            "data": []
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
        await client.GetAllBankPayoutDetailsAsync();

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public async Task GetAllBankPayoutDetailsAsync_ReturnsFailure_WhenRequestFails()
    {
        // Arrange
        var json = """{"message":"Unable to retrieve transactions"}""";

        var handler = new FakeHttpMessageHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(json)
                }));

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetAllBankPayoutDetailsAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("Unable to retrieve transactions", result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetAllBankPayoutDetailsAsync_ReturnsFailure_WhenResponseContainsInvalidJson()
    {
        // Arrange
        var json = """{"status":"success","data":""";

        var handler = new FakeHttpMessageHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                }));

        using var serviceProvider = CreateServiceProvider(handler);
        var client = serviceProvider.GetRequiredService<IPayChanguClient>();

        // Act
        var result = await client.GetAllBankPayoutDetailsAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(json, result.RawJsonResponse);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetAllBankPayoutDetailsAsync_ReturnsEmptyCollection_WhenNoTransactionsExist()
    {
        // Arrange
        var json = """
    {
        "status": "success",
        "message": "Transactions retrieved successfully.",
        "data": {
            "current_page": 1,
            "total_pages": 0,
            "per_page": 20,
            "next_page_url": null,
            "data": []
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
        var result = await client.GetAllBankPayoutDetailsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.NotNull(result.Result.Data);
        Assert.Empty(result.Result.Data.Transactions);
        Assert.Equal(0, result.Result.Data.TotalPages);
    }
}