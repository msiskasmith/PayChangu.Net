using PayChangu.NET.Constants;
using PayChangu.NET.Exceptions;
using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    Task<PayChanguApiResponse<InitiateCheckoutResponse>> InitiatePaymentAsync(
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
    Task<PayChanguApiResponse<VerifyPaymentResponse>> VerifyPaymentAsync(
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
    Task<PayChanguApiResponse<InitiateMobileMoneyDirectChargeResponse>> InitiateMobileMoneyDirectChargeAsync(
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
    Task<PayChanguApiResponse<InitiateBankTransferDirectChargeResponse>> InitiateBankTransferDirectChargeAsync(
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
    Task<PayChanguApiResponse<GetBalanceResponse>> GetBalanceAsync(
        string currency = "MWK",
        CancellationToken cancellationToken = default);
}


/// <summary>
/// Provides the HTTP implementation of <see cref="IPayChanguClient"/>.
/// </summary>
/// <remarks>
/// This implementation handles HTTP communication, authentication through
/// the configured <see cref="HttpClient"/>, JSON serialization, and
/// deserialization when communicating with the PayChangu API.
/// It is registered through dependency injection and is not intended
/// to be instantiated directly by consumers.
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
    public async Task<PayChanguApiResponse<InitiateCheckoutResponse>>
        InitiatePaymentAsync(
            InitiateCheckoutRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            PayChanguApiEndpoints.HostedCheckout.InitiateTransaction);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return await SendAsync<InitiateCheckoutResponse>(
            httpRequest,
            "Standard Checkout initiation",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<VerifyPaymentResponse>>
        VerifyPaymentAsync(
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
                "Checkout verification",
                cancellationToken);

            // Return the original API failure without attempting to map its data.
            if (!response.Success)
            {
                return CopyFailure<VerifyCheckoutPaymentResponse, VerifyPaymentResponse>(
                    response);
            }

            var checkoutData = response.Result?.Data;

            if (checkoutData == null)
            {
                return CreateFailure<VerifyPaymentResponse>(
                    response.StatusCode,
                    "PayChangu returned an empty Checkout verification data payload.",
                    response.RawJsonResponse);
            }

            var verification = MapCheckoutVerificationResponse(
                response.Result!,
                transactionIdentifier,
                paymentMethod);

            return CreateSuccess(
                response.StatusCode,
                verification,
                response.RawJsonResponse);
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

                        // Preserve the API failure and its original response details.
                        if (!response.Success)
                        {
                            return CopyFailure<
                                VerifyMobileMoneyDirectChargeResponse,
                                VerifyPaymentResponse>(response);
                        }

                        var directChargeData = response.Result?.Data;

                        if (directChargeData == null)
                        {
                            return CreateFailure<VerifyPaymentResponse>(
                                response.StatusCode,
                                "PayChangu returned an empty Mobile Money Direct Charge verification data payload.",
                                response.RawJsonResponse);
                        }

                        var verification = MapDirectChargeVerificationResponse(
                            response.Result!.Status,
                            response.Result.Message,
                            directChargeData,
                            transactionIdentifier,
                            paymentMethod);

                        return CreateSuccess(
                            response.StatusCode,
                            verification,
                            response.RawJsonResponse);
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

                        // Preserve the API failure and its original response details.
                        if (!response.Success)
                        {
                            return CopyFailure<
                                VerifyBankTransferDirectChargeResponse,
                                VerifyPaymentResponse>(response);
                        }

                        var transaction = response.Result?.Data?.Transaction;

                        if (transaction == null)
                        {
                            return CreateFailure<VerifyPaymentResponse>(
                                response.StatusCode,
                                "PayChangu returned an empty Bank Transfer Direct Charge verification transaction payload.",
                                response.RawJsonResponse);
                        }

                        try
                        {
                            var verification = MapDirectChargeVerificationResponse(
                                response.Result!.Status,
                                response.Result.Message,
                                transaction,
                                transactionIdentifier,
                                paymentMethod);

                            return CreateSuccess(
                                response.StatusCode,
                                verification,
                                response.RawJsonResponse);
                        }
                        catch (PayChanguException ex)
                        {
                            return CreateFailure<VerifyPaymentResponse>(
                                response.StatusCode,
                                ex.Message,
                                response.RawJsonResponse);
                        }
                    }

                case PayChanguPaymentMethod.Card:
                    {
                        var endpoint = PayChanguApiEndpoints.DirectChargeCard
                            .VerifyDirectChargeStatus
                            .Replace(
                                "{id}",
                                Uri.EscapeDataString(transactionIdentifier));

                        return await GetStatusAsync<VerifyPaymentResponse>(
                            endpoint,
                            "Card Direct Charge verification",
                            cancellationToken);
                    }

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
    public async Task<PayChanguApiResponse<InitiateMobileMoneyDirectChargeResponse>>
        InitiateMobileMoneyDirectChargeAsync(
            InitiateMobileMoneyDirectChargeRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            PayChanguApiEndpoints.MobileMoneyDirectCharge
                .InitiateMobileMoneyDirectCharge);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return await SendAsync<InitiateMobileMoneyDirectChargeResponse>(
            httpRequest,
            "Mobile Money Direct Charge initiation",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<InitiateBankTransferDirectChargeResponse>>
        InitiateBankTransferDirectChargeAsync(
            InitiateBankTransferDirectChargeRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            PayChanguApiEndpoints.BankTransferDirectCharge
                .InitiateBankTransferDirectCharge);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return await SendAsync<InitiateBankTransferDirectChargeResponse>(
            httpRequest,
            "Bank Transfer Direct Charge initiation",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<GetBalanceResponse>>
        GetBalanceAsync(
            string currency = "MWK",
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "Currency cannot be null or empty.",
                nameof(currency));
        }

        var endpoint = PayChanguApiEndpoints.Account.GetBalance.Replace(
            "{iso}",
            Uri.EscapeDataString(currency));

        return await GetAsync<GetBalanceResponse>(
            endpoint,
            "wallet balance retrieval",
            cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP request to PayChangu and deserializes the response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response model type.</typeparam>
    /// <param name="httpRequest">The HTTP request to send.</param>
    /// <param name="operation">A description of the API operation.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A response wrapper containing the deserialized API response on success,
    /// or the HTTP status code, error message, and raw response on failure.
    /// </returns>
    /// <remarks>
    /// HTTP errors, empty response bodies, and JSON deserialization failures
    /// are represented by a failed <see cref="PayChanguApiResponse{T}"/>.
    /// HTTP transport exceptions and cancellation exceptions are not caught
    /// and will propagate to the caller.
    /// </remarks>
    private async Task<PayChanguApiResponse<TResponse>> SendAsync<TResponse>(
        HttpRequestMessage httpRequest,
        string operation,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        using var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync();

        // Return unsuccessful HTTP responses as API response objects.
        if (!response.IsSuccessStatusCode)
        {
            return CreateFailure<TResponse>(
                response.StatusCode,
                $"PayChangu returned an error during {operation} " +
                $"({(int)response.StatusCode}): {content}",
                content);
        }

        // A successful HTTP status does not guarantee a valid response body.
        if (string.IsNullOrWhiteSpace(content))
        {
            return CreateFailure<TResponse>(
                response.StatusCode,
                $"PayChangu returned an empty response during {operation}.",
                content);
        }

        try
        {
            var result = JsonSerializer.Deserialize<TResponse>(
                content,
                _jsonOptions);

            if (result == null)
            {
                return CreateFailure<TResponse>(
                    response.StatusCode,
                    $"PayChangu returned an empty response during {operation}.",
                    content);
            }

            return CreateSuccess(
                response.StatusCode,
                result,
                content);
        }
        catch (JsonException ex)
        {
            return CreateFailure<TResponse>(
                response.StatusCode,
                $"Unable to deserialize the PayChangu response during {operation}: {ex.Message}",
                content);
        }
    }

    /// <summary>
    /// Sends a GET request to PayChangu and deserializes the response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response model type.</typeparam>
    /// <param name="endpoint">The API endpoint to request.</param>
    /// <param name="operation">A description of the API operation.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A response wrapper containing the deserialized API response or failure details.
    /// </returns>
    private async Task<PayChanguApiResponse<TResponse>> GetAsync<TResponse>(
        string endpoint,
        string operation,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        return await SendAsync<TResponse>(
            httpRequest,
            operation,
            cancellationToken);
    }

    /// <summary>
    /// Sends a GET request and evaluates its success using the HTTP status code
    /// without attempting to deserialize the response body.
    /// </summary>
    /// <typeparam name="TResponse">The expected response data type.</typeparam>
    /// <param name="endpoint">The API endpoint to send the request to.</param>
    /// <param name="operation">A description of the operation being performed.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A response indicating whether the request was successful and the HTTP
    /// status code returned by PayChangu.
    /// </returns>
    private async Task<PayChanguApiResponse<TResponse>> GetStatusAsync<TResponse>(
        string endpoint,
        string operation,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return CreateSuccess<TResponse>(
                response.StatusCode,
                null);
        }

        var errorMessage =
            $"{operation} failed with HTTP status code {(int)response.StatusCode}.";

        return CreateFailure<TResponse>(
            response.StatusCode,
            errorMessage,
            null);
    }

    /// <summary>
    /// Creates a successful API response wrapper.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the API.</typeparam>
    /// <param name="statusCode">The HTTP status code returned by PayChangu.</param>
    /// <param name="data">The deserialized response data.</param>
    /// <param name="rawJsonResponse">The original JSON response body.</param>
    /// <returns>A successful API response wrapper.</returns>
    private static PayChanguApiResponse<T> CreateSuccess<T>(
        HttpStatusCode? statusCode,
        T data,
        string? rawJsonResponse)
    {
        return new PayChanguApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Result = data,
            RawJsonResponse = rawJsonResponse
        };
    }

    /// <summary>
    /// Creates a successful API response wrapper.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the API.</typeparam>
    /// <param name="statusCode">The HTTP status code returned by PayChangu.</param>
    /// <param name="rawJsonResponse">The original JSON response body.</param>
    /// <returns>A successful API response wrapper.</returns>
    private static PayChanguApiResponse<T> CreateSuccess<T>(
        HttpStatusCode? statusCode,
        string? rawJsonResponse)
    {
        return new PayChanguApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            RawJsonResponse = rawJsonResponse
        };
    }

    /// <summary>
    /// Creates a failed API response wrapper.
    /// </summary>
    /// <typeparam name="T">The expected type of data returned by the API.</typeparam>
    /// <param name="statusCode">The HTTP status code, if available.</param>
    /// <param name="errorMessage">The reason the API operation failed.</param>
    /// <param name="rawJsonResponse">The original response body, if available.</param>
    /// <returns>A failed API response wrapper.</returns>
    private static PayChanguApiResponse<T> CreateFailure<T>(
        HttpStatusCode? statusCode,
        string errorMessage,
        string? rawJsonResponse)
    {
        return new PayChanguApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            RawJsonResponse = rawJsonResponse
        };
    }

    /// <summary>
    /// Copies a failed API response into a wrapper with a different data type.
    /// </summary>
    /// <typeparam name="TSource">The original response data type.</typeparam>
    /// <typeparam name="TResult">The target response data type.</typeparam>
    /// <param name="response">The original failed API response.</param>
    /// <returns>A failed API response using the target data type.</returns>
    private static PayChanguApiResponse<TResult>
        CopyFailure<TSource, TResult>(
            PayChanguApiResponse<TSource> response)
    {
        return CreateFailure<TResult>(
            response.StatusCode,
            response.ErrorMessage ?? "The PayChangu API request failed.",
            response.RawJsonResponse);
    }

    /// <summary>
    /// Maps a PayChangu Checkout verification response to the normalized
    /// verification response model.
    /// </summary>
    /// <param name="response">The original Checkout verification response.</param>
    /// <param name="transactionIdentifier">The merchant transaction reference.</param>
    /// <param name="paymentMethod">The payment method used for the transaction.</param>
    /// <returns>The normalized payment verification response.</returns>
    private static VerifyPaymentResponse MapCheckoutVerificationResponse(
        VerifyCheckoutPaymentResponse response,
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
            CompletedAt = data.Authorization?.CompletedAt
        };
    }

    /// <summary>
    /// Maps a PayChangu Direct Charge transaction to the normalized
    /// verification response model.
    /// </summary>
    /// <param name="apiStatus">The status of the PayChangu API response.</param>
    /// <param name="message">The message returned by PayChangu.</param>
    /// <param name="transaction">The Direct Charge transaction details.</param>
    /// <param name="transactionIdentifier">The charge identifier used for verification.</param>
    /// <param name="paymentMethod">The payment method used for the transaction.</param>
    /// <returns>The normalized payment verification response.</returns>
    private static VerifyPaymentResponse MapDirectChargeVerificationResponse(
        string apiStatus,
        string message,
        PayChanguDirectChargeTransaction transaction,
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
            CompletedAt = transaction.CompletedAt
        };
    }

    /// <summary>
    /// Parses a transaction charge amount using invariant culture.
    /// </summary>
    /// <param name="amount">The charge amount returned by PayChangu.</param>
    /// <returns>The parsed charge amount.</returns>
    /// <exception cref="PayChanguException">
    /// Thrown when the charge amount cannot be parsed as a decimal value.
    /// </exception>
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
}