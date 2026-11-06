using PayChangu.NET.Exceptions;
using PayChangu.NET.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PayChangu.NET.Constants;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace PayChangu.NET;

/// <summary>
/// Provides methods for interacting with the PayChangu payment gateway.
/// </summary>
/// <remarks>
/// Implementations handle HTTP communication, authentication, and JSON
/// serialization when communicating with the PayChangu API.
/// Register the client using <see cref="PayChanguServiceCollectionExtensions.AddPayChangu"/>.
/// </remarks>
public interface IPayChanguClient
{
    /// <summary>
    /// Initiates a payment using PayChangu Standard Checkout.
    /// </summary>
    /// <param name="request">
    /// The payment details, including the amount, customer information,
    /// transaction reference, and checkout configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="InitiateCheckoutResponse"/> containing the checkout URL and
    /// details of the created payment session.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="PayChanguException">
    /// Thrown when PayChangu returns an unsuccessful HTTP response or
    /// when the response cannot be deserialized.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled through the
    /// <paramref name="cancellationToken"/>.
    /// </exception>
    Task<InitiateCheckoutResponse> InitiatePaymentAsync(
        InitiateCheckoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a PayChangu payment using its transaction reference or charge identifier.
    /// Supports Standard Checkout and Mobile Money or Bank Transfer Direct Charge.
    /// </summary>
    /// <param name="transactionIdentifier">
    /// The merchant transaction reference for Checkout, or the charge identifier for Direct Charge.
    /// </param>
    /// <param name="paymentFlow">The payment flow used to initiate the payment.</param>
    /// <param name="paymentMethod">The payment method used for the payment.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A normalized verification response containing the payment details.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the transaction identifier is null, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an unsupported payment flow or method is supplied.
    /// </exception>
    /// <exception cref="PayChanguException">
    /// Thrown when PayChangu returns an unsuccessful response or an invalid response payload.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    /// /// <exception cref="NotSupportedException">
    /// Thrown when Card is supplied with the Direct Charge payment flow,
    /// which is not currently supported.
    /// </exception>
    Task<VerifyPaymentResponse> VerifyPaymentAsync(
        string transactionIdentifier,
        PayChanguPaymentFlow paymentFlow,
        PayChanguPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a direct mobile money payment request.
    /// </summary>
    /// <param name="request">
    /// The mobile money charge details.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the HTTP request.
    /// </param>
    /// <returns>
    /// The response indicating whether PayChangu accepted the charge request
    /// and initiated the payment process. It does not confirm payment completion.
    /// </returns>
    /// /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="PayChanguException">
    /// Thrown when PayChangu returns an unsuccessful HTTP response
    /// or when the response cannot be deserialized.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled through the
    /// <paramref name="cancellationToken"/>.
    /// </exception>
    Task<InitiateMobileMoneyDirectChargeResponse> InitiateMobileMoneyDirectChargeAsync(
        InitiateMobileMoneyDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a direct bank transfer payment request.
    /// </summary>
    /// <param name="request">
    /// The bank transfer charge details.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the HTTP request.
    /// </param>
    /// <returns>
    /// The response containing the bank account details and transaction
    /// information required to complete the payment.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="PayChanguException">
    /// Thrown when PayChangu returns an unsuccessful HTTP response
    /// or when the response cannot be deserialized.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled through the
    /// <paramref name="cancellationToken"/>.
    /// </exception>
    Task<InitiateBankTransferDirectChargeResponse> InitiateBankTransferDirectChargeAsync(
        InitiateBankTransferDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the wallet balance for the specified currency.
    /// </summary>
    /// <param name="currency">
    /// The wallet currency. Defaults to MWK when not specified.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>The wallet balance details.</returns>
    Task<GetBalanceResponse> GetBalanceAsync(
        string currency = "MWK",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides the HTTP implementation of <see cref="IPayChanguClient"/>.
/// </summary>
/// <remarks>
/// This implementation is registered through dependency injection
/// and is not intended to be instantiated directly by consumers.
/// </remarks>
internal sealed class PayChanguClient : IPayChanguClient
{
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PayChanguClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for API communication.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public PayChanguClient(HttpClient httpClient)
    {
        _httpClient = httpClient ??
            throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<InitiateCheckoutResponse> InitiatePaymentAsync(
        InitiateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            PayChanguApiEndpoints.HostedCheckout.InitiateTransaction);

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new PayChanguException(
                $"PayChangu returned an error ({(int)response.StatusCode}): {content}",
                response.StatusCode,
                content);
        }

        try
        {
            var result = JsonSerializer.Deserialize<InitiateCheckoutResponse>(
                content,
                _jsonOptions);

            return result ?? throw new PayChanguException(
                "PayChangu returned an empty response.");
        }
        catch (JsonException ex)
        {
            throw new PayChanguException(
                "Unable to deserialize PayChangu response.",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(
        string transactionIdentifier,
        PayChanguPaymentFlow paymentFlow,
        PayChanguPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionIdentifier))
        {
            throw new ArgumentException(
                "Transaction identifier is required.",
                nameof(transactionIdentifier));
        }

        if (paymentFlow == PayChanguPaymentFlow.Checkout)
        {
            if (paymentMethod is not (
                PayChanguPaymentMethod.MobileMoney or
                PayChanguPaymentMethod.BankTransfer or
                PayChanguPaymentMethod.Card))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(paymentMethod),
                    paymentMethod,
                    "Unsupported payment method.");
            }

            var endpoint = PayChanguApiEndpoints.HostedCheckout.VerifyTransactionStatus
                .Replace(
                    "{ref}",
                    Uri.EscapeDataString(transactionIdentifier));

            var response = await GetAsync<VerifyCheckoutPaymentResponse>(
                endpoint,
                "verification",
                cancellationToken);

            if (response.Data.Data == null)
            {
                throw new PayChanguException(
                    "PayChangu returned an empty Checkout verification data payload.");
            }

            return MapCheckoutVerificationResponse(
                response.Data,
                response.RawJsonResponse,
                transactionIdentifier,
                paymentMethod);
        }

        if (paymentFlow == PayChanguPaymentFlow.DirectCharge)
        {
            switch (paymentMethod)
            {
                case PayChanguPaymentMethod.MobileMoney:
                    {
                        var endpoint = PayChanguApiEndpoints.MobileMoneyDirectCharge
                            .VerifyDirectChargeStatus
                            .Replace(
                                "{id}",
                                Uri.EscapeDataString(transactionIdentifier));

                        var response =
                            await GetAsync<VerifyMobileMoneyDirectChargeResponse>(
                                endpoint,
                                "Mobile Money Direct Charge verification",
                                cancellationToken);

                        if (response.Data.Data == null)
                        {
                            throw new PayChanguException(
                                "PayChangu returned an empty Mobile Money Direct Charge verification data payload.");
                        }

                        return MapDirectChargeVerificationResponse(
                            response.Data.Status,
                            response.Data.Message,
                            response.Data.Data,
                            response.RawJsonResponse,
                            transactionIdentifier,
                            paymentMethod);
                    }

                case PayChanguPaymentMethod.BankTransfer:
                    {
                        var endpoint = PayChanguApiEndpoints.BankTransferDirectCharge
                            .GetSingleChargeDetails
                            .Replace(
                                "{id}",
                                Uri.EscapeDataString(transactionIdentifier));

                        var response =
                            await GetAsync<VerifyBankTransferDirectChargeResponse>(
                                endpoint,
                                "Bank Transfer Direct Charge verification",
                                cancellationToken);

                        if (response.Data.Data?.Transaction == null)
                        {
                            throw new PayChanguException(
                                "PayChangu returned an empty Bank Transfer Direct Charge verification transaction payload.");
                        }

                        return MapDirectChargeVerificationResponse(
                            response.Data.Status,
                            response.Data.Message,
                            response.Data.Data.Transaction,
                            response.RawJsonResponse,
                            transactionIdentifier,
                            paymentMethod);
                    }

                case PayChanguPaymentMethod.Card:
                    throw new NotSupportedException(
                        "Card Direct Charge verification is not yet supported.");

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(paymentMethod),
                        paymentMethod,
                        "Unsupported Direct Charge payment method.");
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(paymentFlow),
            paymentFlow,
            "Only Checkout and Direct Charge payment flows are supported.");
    }

    /// <inheritdoc />
    public async Task<InitiateMobileMoneyDirectChargeResponse> InitiateMobileMoneyDirectChargeAsync(
        InitiateMobileMoneyDirectChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(
            PayChanguApiEndpoints.MobileMoneyDirectCharge.InitiateMobileMoneyDirectCharge,
            content,
            cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new PayChanguException(
                "Failed to initiate mobile money charge.",
                response.StatusCode,
                responseContent);
        }

        try
        {
            var result = JsonSerializer.Deserialize<InitiateMobileMoneyDirectChargeResponse>(
                        responseContent,
                        _jsonOptions);

            return result ?? throw new PayChanguException(
                "PayChangu returned an empty mobile money charge response.");
        }
        catch (JsonException ex)
        {
            throw new PayChanguException(
                "Failed to deserialize the mobile money charge response.",
                ex);
        }
    }

    public async Task<InitiateBankTransferDirectChargeResponse> InitiateBankTransferDirectChargeAsync(
    InitiateBankTransferDirectChargeRequest request,
    CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(
            PayChanguApiEndpoints.BankTransferDirectCharge.InitiateBankTransferDirectCharge,
            content,
            cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new PayChanguException(
                "Failed to initiate bank transfer charge.",
                response.StatusCode,
                responseContent);
        }

        try
        {
            var result = JsonSerializer.Deserialize<InitiateBankTransferDirectChargeResponse>(
                responseContent,
                _jsonOptions);

            return result ?? throw new PayChanguException(
                "PayChangu returned an empty bank transfer charge response.");
        }
        catch (JsonException ex)
        {
            throw new PayChanguException(
                "Failed to deserialize the bank transfer charge response.",
                ex);
        }
    }

    private async Task<PayChanguApiResponse<TResponse>> GetAsync<TResponse>(
    string endpoint,
    string operation,
    CancellationToken cancellationToken)
    where TResponse : class
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new PayChanguException(
                $"PayChangu returned an error during {operation} " +
                $"({(int)response.StatusCode}): {content}",
                response.StatusCode,
                content);
        }

        try
        {
            var result = JsonSerializer.Deserialize<TResponse>(
                    content,
                    _jsonOptions);

            if (result == null)
            {
                throw new PayChanguException(
                    $"PayChangu returned an empty response during {operation}.");
            }

            return new PayChanguApiResponse<TResponse>
            {
                Data = result,
                RawJsonResponse = content
            };
        }
        catch (JsonException ex)
        {
            throw new PayChanguException(
                $"Unable to deserialize PayChangu response during {operation}.",
                ex);
        }
    }

    private static VerifyPaymentResponse MapCheckoutVerificationResponse(
    VerifyCheckoutPaymentResponse response,
    string rawJsonResponse,
    string transactionIdentifier,
    PayChanguPaymentMethod paymentMethod)
    {
        var data = response.Data;

        return new VerifyPaymentResponse
        {
            PaymentFlow = PayChanguPaymentFlow.Checkout,
            PaymentMethod = paymentMethod,

            TransactionIdentifier = transactionIdentifier,
            TransactionReference = data.TransactionReference,

            ProviderReference = data.ProviderReference,

            EventType = data.EventType,
            Mode = data.Mode,
            Type = data.Type,

            Status = data.Status,
            ApiStatus = response.Status,
            Message = response.Message,

            Attempts = data.NumberOfAttempts,

            Currency = data.Currency,
            Amount = data.Amount,

            Charges = new PayChanguVerificationCharges
            {
                Currency = data.Currency,
                Amount = data.Charges
            },

            Customization = data.Customization == null
                ? null
                : new VerificationCustomization
                {
                    Title = data.Customization.Title,
                    Description = data.Customization.Description,
                    Logo = data.Customization.Logo
                },

            Meta = data.Meta,

            Authorization = data.Authorization == null
                ? null
                : new VerificationAuthorization
                {
                    Channel = data.Authorization.Channel,
                    CardNumber = data.Authorization.MaskedCardNumber,
                    Expiry = data.Authorization.Expiry,
                    Brand = data.Authorization.Brand,
                    Provider = data.Authorization.Provider,
                    MobileNumber = data.Authorization.MobileNumber,
                    CompletedAt = data.Authorization.CompletedAt
                },

            Customer = data.Customer == null
                ? null
                : new VerificationCustomer
                {
                    Email = data.Customer.Email,
                    FirstName = data.Customer.FirstName,
                    LastName = data.Customer.LastName
                },

            Logs = data.Logs?
                .Select(log => new VerificationLog
                {
                    Type = log.Type,
                    Message = log.Message,
                    CreatedAt = log.CreatedAt
                })
                .ToList() ?? new List<VerificationLog>(),

            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            CompletedAt = data.Authorization?.CompletedAt,
            RawJsonResponse = rawJsonResponse
        };
    }

    private static VerifyPaymentResponse MapDirectChargeVerificationResponse(
    string apiStatus,
    string message,
    PayChanguDirectChargeTransaction transaction,
    string rawJsonResponse,
    string transactionIdentifier,
    PayChanguPaymentMethod paymentMethod)
    {
        return new VerifyPaymentResponse
        {
            PaymentFlow = PayChanguPaymentFlow.DirectCharge,
            PaymentMethod = paymentMethod,

            TransactionIdentifier = transactionIdentifier,
            TransactionReference = transaction.ReferenceId,
            ChargeId = transaction.ChargeId,
            TransactionId = transaction.TransactionId,
            TraceId = transaction.TraceId,

            EventType = transaction.EventType,
            Mode = transaction.Mode,
            Type = transaction.Type,

            Status = transaction.Status,
            ApiStatus = apiStatus,
            Message = message,

            Attempts = transaction.Attempts,

            Currency = transaction.Currency,
            Amount = transaction.Amount,

            Charges = transaction.TransactionCharges == null
                ? null
                : new PayChanguVerificationCharges
                {
                    Currency = transaction.TransactionCharges.Currency,
                    Amount = ParseChargeAmount(
                        transaction.TransactionCharges.Amount)
                },

            Authorization = transaction.Authorization == null
                ? null
                : new VerificationAuthorization
                {
                    Channel = transaction.Authorization.Channel,
                    CardNumber = transaction.Authorization.CardNumber,
                    Expiry = transaction.Authorization.Expiry,
                    Brand = transaction.Authorization.Brand,
                    Provider = transaction.Authorization.Provider,
                    MobileNumber = transaction.Authorization.MobileNumber,
                    PayerBankUuid = transaction.Authorization.PayerBankUuid,
                    PayerBank = transaction.Authorization.PayerBank,
                    PayerAccountNumber =
                        transaction.Authorization.PayerAccountNumber,
                    PayerAccountName =
                        transaction.Authorization.PayerAccountName,
                    CompletedAt = transaction.Authorization.CompletedAt
                },

            MobileMoney = transaction.MobileMoney,

            Customer = transaction.FirstName == null &&
                       transaction.LastName == null &&
                       transaction.Email == null &&
                       transaction.Mobile == null
                ? null
                : new VerificationCustomer
                {
                    FirstName = transaction.FirstName,
                    LastName = transaction.LastName,
                    Email = transaction.Email,
                    Mobile = transaction.Mobile
                },

            Logs = transaction.Logs?
                .Select(log => new VerificationLog
                {
                    Type = log.Type,
                    Message = log.Message,
                    CreatedAt = log.CreatedAt
                })
                .ToList() ?? new List<VerificationLog>(),

            CreatedAt = transaction.CreatedAt,
            CompletedAt = transaction.CompletedAt,
            RawJsonResponse = rawJsonResponse
        };
    }

    private static decimal ParseChargeAmount(string amount)
    {
        if (!decimal.TryParse(
            amount,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var result))
        {
            throw new PayChanguException(
                $"PayChangu returned an invalid transaction charge amount: {amount}");
        }

        return result;
    }

    public async Task<GetBalanceResponse> GetBalanceAsync(
    string currency = "MWK",
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException(
                "Currency cannot be null or empty.",
                nameof(currency));

        var endpoint = PayChanguApiEndpoints.Account.GetBalance.Replace(
            "{iso}",
            Uri.EscapeDataString(currency));

        using var response = await _httpClient.GetAsync(
            endpoint,
            cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new PayChanguException(
                "Failed to retrieve wallet balance.",
                response.StatusCode,
                responseContent);
        }

        try
        {
            var result = JsonSerializer.Deserialize<GetBalanceResponse>(
                responseContent,
                _jsonOptions);

            return result ?? throw new PayChanguException(
                "PayChangu returned an empty wallet balance response.");
        }
        catch (JsonException ex)
        {
            throw new PayChanguException(
                "Failed to deserialize the wallet balance response.",
                ex);
        }
    }
}

internal sealed class PayChanguApiResponse<TResponse>
{
    public TResponse Data { get; set; } = default!;

    public string RawJsonResponse { get; set; } = null!;
}