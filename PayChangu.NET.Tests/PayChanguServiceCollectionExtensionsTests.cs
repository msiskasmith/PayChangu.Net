using Microsoft.Extensions.DependencyInjection;
using PayChangu.NET.Extensions;
using PayChangu.NET.Tests.Fakes;
using System.Net;

namespace PayChangu.NET.Tests;

public sealed class PayChanguServiceCollectionExtensionsTests
{
    private const string SecretKey = "test-secret-key";
    private const string BaseUrl = "https://sandbox.example.com/api/";

    private static HttpResponseMessage CreateSuccessResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
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
        };
    }

    [Fact]
    public async Task AddPayChangu_ShouldRegisterClientAndConfigureHttpClient()
    {
        var handler = new FakeHttpMessageHandler(CreateSuccessResponse());

        var services = new ServiceCollection();

        services.AddPayChangu(
            options =>
            {
                options.SecretKey = SecretKey;
                options.BaseUrl = BaseUrl;
            },
            httpClientBuilder =>
            {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(
                    () => handler);
            });

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IPayChanguClient>();

        Assert.NotNull(client);
        Assert.NotNull(client.Checkout);
        Assert.NotNull(client.DirectCharges.MobileMoney);
        Assert.NotNull(client.DirectCharges.BankTransfer);
        Assert.NotNull(client.DirectCharges.Card);
        Assert.NotNull(client.Payouts.MobileMoney);
        Assert.NotNull(client.Payouts.Bank);
        Assert.NotNull(client.Transactions);
        Assert.NotNull(client.Wallet);
        Assert.NotNull(client.ReferenceData.MobileMoneyOperators);
        Assert.NotNull(client.ReferenceData.Banks);

        await client.Checkout.InitiateAsync(new Models.InitiateCheckoutRequest
        {
            Amount = 5000,
            Currency = PayChanguCurrency.MWK,
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Banda",
            CallbackUrl = "https://example.com/callback",
            ReturnUrl = "https://example.com/return",
            TransactionReference = "test-transaction-001",
            Customization = new Models.PaymentCustomization
            {
                Title = "Test payment",
                Description = "Testing DI registration"
            },
            Meta = new Models.PaymentMeta
            {
                Id = Guid.Parse("82310dd1-ec9b-4fe7-a32c-2f262ef08681"),
                Response = "test-response"
            }
        });

        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(
            "https://sandbox.example.com/api/payment",
            handler.RequestUri!.ToString());

        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(SecretKey, handler.AuthorizationParameter);
    }

    [Fact]
    public void AddPayChangu_ShouldThrowArgumentException_WhenSecretKeyIsMissing()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(
            () => services.AddPayChangu(options =>
            {
                options.BaseUrl = BaseUrl;
            }));

        Assert.Contains("SecretKey", exception.Message);
    }

    [Fact]
    public void AddPayChangu_ShouldThrowArgumentException_WhenBaseUrlIsInvalid()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(
            () => services.AddPayChangu(options =>
            {
                options.SecretKey = SecretKey;
                options.BaseUrl = "not-a-valid-url";
            }));

        Assert.Contains("BaseUrl", exception.Message);
    }

    [Fact]
    public void AddPayChangu_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddPayChangu(options =>
            {
                options.SecretKey = SecretKey;
            }));
    }

    [Fact]
    public void AddPayChangu_ShouldThrowArgumentNullException_WhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddPayChangu(null!));
    }
}