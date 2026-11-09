using PayChangu.NET.Constants;
using PayChangu.NET.Exceptions;
using PayChangu.NET.Extensions;
using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>
/// Provides methods for interacting with the PayChangu payment gateway.
/// </summary>
/// <remarks>
/// API-level failures, including unsuccessful HTTP responses and response
/// deserialization failures, are returned through the corresponding
/// <see cref="PayChanguApiResponse"/> wrapper.
/// Exceptions are reserved for argument validation, explicitly documented
/// validation failures, HTTP transport errors, and cancellation.
/// Register the client using
/// <see cref="PayChanguServiceCollectionExtensions.AddPayChangu"/>.
/// </remarks>
/// <summary>
/// Provides grouped access to the PayChangu API by business capability.
/// </summary>
public interface IPayChanguClient
{
    /// <summary>Gets the Standard Checkout operations.</summary>
    IPayChanguCheckoutClient Checkout { get; }

    /// <summary>Gets the Direct Charge operations grouped by payment method.</summary>
    IPayChanguDirectChargesClient DirectCharges { get; }

    /// <summary>Gets the payout operations grouped by payout method.</summary>
    IPayChanguPayoutsClient Payouts { get; }

    /// <summary>Gets the transaction operations.</summary>
    IPayChanguTransactionsClient Transactions { get; }

    /// <summary>Gets the wallet operations.</summary>
    IPayChanguWalletClient Wallet { get; }

    /// <summary>Gets the PayChangu reference data operations.</summary>
    IPayChanguReferenceDataClient ReferenceData { get; }
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
    private readonly ILogger<PayChanguClient> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PayChanguClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for API communication.</param>
    /// <param name="logger">The logger used to record SDK diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> or <paramref name="logger"/>
    /// is <see langword="null"/>.
    /// </exception>
    public PayChanguClient(
        HttpClient httpClient,
        ILogger<PayChanguClient> logger)
    {
        _httpClient = httpClient ??
            throw new ArgumentNullException(nameof(httpClient));

        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));

        Checkout = new PayChanguCheckoutClient(this);
        DirectCharges = new PayChanguDirectChargesClient(this);
        Payouts = new PayChanguPayoutsClient(this);
        Transactions = new PayChanguTransactionsClient(this);
        Wallet = new PayChanguWalletClient(this);
        ReferenceData = new PayChanguReferenceDataClient(this);
    }

    /// <inheritdoc />
    public IPayChanguCheckoutClient Checkout { get; }

    /// <inheritdoc />
    public IPayChanguDirectChargesClient DirectCharges { get; }

    /// <inheritdoc />
    public IPayChanguPayoutsClient Payouts { get; }

    /// <inheritdoc />
    public IPayChanguTransactionsClient Transactions { get; }

    /// <inheritdoc />
    public IPayChanguWalletClient Wallet { get; }

    /// <inheritdoc />
    public IPayChanguReferenceDataClient ReferenceData { get; }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<InitiateCheckoutResponse>>
        InitiateCheckoutAsync(
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

            var response = await GetAsync<ApiVerifyPaymentResponse>(
                endpoint,
                "Checkout verification",
                cancellationToken);

            // Return the original API failure without attempting to map its data.
            if (!response.Success)
            {
                return CopyFailure<ApiVerifyPaymentResponse, VerifyPaymentResponse>(
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

            var verification = MapApiVerificationResponse(
                response.Result!,
                transactionIdentifier,
                paymentMethod,
                paymentFlow);

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
                            await GetAsync<ApiVerifyMobileMoneyDirectChargeResponse>(
                                endpoint,
                                "Mobile Money Direct Charge verification",
                                cancellationToken);

                        // Preserve the API failure and its original response details.
                        if (!response.Success)
                        {
                            return CopyFailure<
                                ApiVerifyMobileMoneyDirectChargeResponse,
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
                            await GetAsync<ApiVerifyBankTransferDirectChargeResponse>(
                                endpoint,
                                "Bank Transfer Direct Charge verification",
                                cancellationToken);

                        // Preserve the API failure and its original response details.
                        if (!response.Success)
                        {
                            return CopyFailure<
                                ApiVerifyBankTransferDirectChargeResponse,
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
                            .VerifyCharge
                            .Replace(
                                "{id}",
                                Uri.EscapeDataString(transactionIdentifier));

                        var response = await GetAsync<ApiVerifyPaymentResponse>(
                            endpoint,
                            "Card Direct Charge verification",
                            cancellationToken);

                        // Return the original API failure without attempting to map its data.
                        if (!response.Success)
                        {
                            return CopyFailure<ApiVerifyPaymentResponse, VerifyPaymentResponse>(
                                response);
                        }

                        var directCardChargeData = response.Result?.Data;

                        if (directCardChargeData == null)
                        {
                            return CreateFailure<VerifyPaymentResponse>(
                                response.StatusCode,
                                "PayChangu returned an empty direct card charge verification data payload.",
                                response.RawJsonResponse);
                        }

                        var verification = MapApiVerificationResponse(
                            response.Result!,
                            transactionIdentifier,
                            paymentMethod,
                            paymentFlow);

                        return CreateSuccess(
                            response.StatusCode,
                            verification,
                            response.RawJsonResponse);
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
    public async Task<PayChanguApiResponse<GetMobileMoneyOperatorsResponse>>
        GetMobileMoneyOperatorsAsync(
            CancellationToken cancellationToken = default)
    {
        return await GetAsync<GetMobileMoneyOperatorsResponse>(
            PayChanguApiEndpoints.MobileMoneyDirectCharge.GetMobileMoneyOperators,
            "mobile money operators retrieval",
            cancellationToken);
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
    public async Task<PayChanguApiResponse<GetMobileMoneyChargeDetailsResponse>>
        GetSingleMobileMoneyChargeDetailsAsync(
            string chargeId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            throw new PayChanguException(
                "The charge ID cannot be null, empty, or whitespace.");
        }

        var endpoint = PayChanguApiEndpoints.MobileMoneyDirectCharge
            .GetSingleChargeDetails
            .Replace("{id}", Uri.EscapeDataString(chargeId));

        return await GetAsync<GetMobileMoneyChargeDetailsResponse>(
            endpoint,
            "mobile money direct charge details retrieval",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<InitiateMobileMoneyPayoutResponse>>
        InitiateMobileMoneyPayoutAsync(
            InitiateMobileMoneyPayoutRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            PayChanguApiEndpoints.Payout.MobileMoneyPayout);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return await SendAsync<InitiateMobileMoneyPayoutResponse>(
            httpRequest,
            "Mobile Money Payout initiation",
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
    public async Task<PayChanguApiResponse<GetBankTransferDirectChargeDetailsResponse>>
        GetSingleBankTransferDirectChargeDetailsAsync(
            string chargeId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            throw new PayChanguException(
                "The charge ID cannot be null, empty, or whitespace.");
        }

        var endpoint = PayChanguApiEndpoints.BankTransferDirectCharge
            .GetSingleChargeDetails
            .Replace("{id}", Uri.EscapeDataString(chargeId));

        return await GetAsync<GetBankTransferDirectChargeDetailsResponse>(
            endpoint,
            "bank transfer direct charge details retrieval",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<InitiateCardDirectChargeResponse>>
        InitiateCardChargeAsync(
            InitiateCardDirectChargeRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            PayChanguApiEndpoints.DirectChargeCard.InitiateCharge);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return await SendAsync<InitiateCardDirectChargeResponse>(
            httpRequest,
            "Card Direct Charge initiation",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<InitiateBankPayoutResponse>>
        InitiateBankPayoutAsync(
            InitiateBankPayoutRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            PayChanguApiEndpoints.Payout.BankPayout);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return await SendAsync<InitiateBankPayoutResponse>(
            httpRequest,
            "Bank Payout initiation",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<GetSingleBankPayoutDetailsResponse>>
        GetSingleBankPayoutDetailsAsync(
            string chargeId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            throw new PayChanguException(
                "The charge ID cannot be null, empty, or whitespace.");
        }

        var endpoint = PayChanguApiEndpoints.Payout
            .GetSingleBankPayoutDetails
            .Replace("{id}", Uri.EscapeDataString(chargeId));

        return await GetAsync<GetSingleBankPayoutDetailsResponse>(
            endpoint,
            "Bank Payout details retrieval",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<GetAllBankPayoutDetailsResponse>>
        GetAllBankPayoutDetailsAsync(
            CancellationToken cancellationToken = default)
    {
        return await GetAsync<GetAllBankPayoutDetailsResponse>(
            PayChanguApiEndpoints.Payout.GetAllBankPayoutDetails,
            "All Bank Payout details retrieval",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse> RefundCardChargeAsync(
        string chargeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            throw new PayChanguException(
                "The charge ID cannot be null, empty, or whitespace.");
        }

        var endpoint = PayChanguApiEndpoints.DirectChargeCard
            .RefundCharge
            .Replace("{id}", Uri.EscapeDataString(chargeId));

        return await PostStatusAsync(
            endpoint,
            "Card Direct Charge refund",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<GetBalanceResponse>>
        GetBalanceAsync(
            PayChanguCurrency currency = PayChanguCurrency.MWK,
            CancellationToken cancellationToken = default)
    {
        var endpoint = PayChanguApiEndpoints.Account.GetBalance.Replace(
            "{iso}",
            Uri.EscapeDataString(currency.ToString()));

        return await GetAsync<GetBalanceResponse>(
            endpoint,
            "wallet balance retrieval",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayChanguApiResponse<GetSupportedBanksResponse>>
        GetSupportedBanksAsync(
            PayChanguCurrency currency = PayChanguCurrency.MWK,
            CancellationToken cancellationToken = default)
        {
            var endpoint = PayChanguApiEndpoints.Payout.GetBanks
                .Replace("{iso}", Uri.EscapeDataString(currency.ToString()));

            return await GetAsync<GetSupportedBanksResponse>(
                endpoint,
                "Supported banks retrieval",
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
    /// HTTP transport exceptions are logged and rethrown. Cancellation
    /// exceptions are not caught and propagate to the caller.
    /// </remarks>
    private async Task<HttpResponseMessage> SendHttpRequestAsync(
        HttpRequestMessage httpRequest,
        string operation,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Starting PayChangu API operation: {Operation}.",
            operation);

        try
        {
            return await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP transport failure during PayChangu operation: {Operation}.",
                operation);

            throw;
        }
    }

    private async Task<PayChanguApiResponse<TResponse>> SendAsync<TResponse>(
        HttpRequestMessage httpRequest,
        string operation,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        using var response = await SendHttpRequestAsync(
            httpRequest,
            operation,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync();

        // Return unsuccessful HTTP responses as API response objects.
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "PayChangu API operation {Operation} returned HTTP status code {StatusCode}.",
                operation,
                (int)response.StatusCode);

            return CreateFailure<TResponse>(
                response.StatusCode,
                ExtractErrorMessage(
                    content,
                    operation,
                    response.StatusCode),
                content);
        }

        // A successful HTTP status does not guarantee a valid response body.
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning(
                "PayChangu returned an empty response during operation {Operation}.",
                operation);

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
                _logger.LogWarning(
                    "PayChangu returned an empty response during operation {Operation}.",
                    operation);

                return CreateFailure<TResponse>(
                    response.StatusCode,
                    $"PayChangu returned an empty response during {operation}.",
                    content);
            }

            _logger.LogDebug(
                "PayChangu API operation {Operation} completed successfully with HTTP status code {StatusCode}.",
                operation,
                (int)response.StatusCode);

            return CreateSuccess(
                response.StatusCode,
                result,
                content);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to deserialize the PayChangu response during operation {Operation}.",
                operation);

            return CreateFailure<TResponse>(
                response.StatusCode,
                $"Unable to deserialize the PayChangu response during {operation}: {ex.Message}",
                content);
        }
    }

    /// <summary>
    /// Extracts the error message from a PayChangu API response.
    /// </summary>
    /// <param name="content">The raw response body.</param>
    /// <param name="operation">A description of the API operation.</param>
    /// <param name="statusCode">The HTTP status code returned by PayChangu.</param>
    /// <returns>
    /// The error message returned by PayChangu, or a fallback message
    /// containing the HTTP status code and response body.
    /// </returns>
    private static string ExtractErrorMessage(
        string content,
        string operation,
        HttpStatusCode statusCode)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var document = JsonDocument.Parse(content);

                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty(
                        "message",
                        out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    var message = messageElement.GetString();

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message!;
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to the original response content.
            }
        }

        return $"PayChangu returned an error during {operation} " +
               $"({(int)statusCode}): {content}";
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

        using var response = await SendHttpRequestAsync(
            request,
            operation,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogDebug(
                "PayChangu API operation {Operation} completed successfully with HTTP status code {StatusCode}.",
                operation,
                (int)response.StatusCode);

            return CreateSuccess<TResponse>(
                response.StatusCode,
                null);
        }

        _logger.LogWarning(
            "PayChangu API operation {Operation} returned HTTP status code {StatusCode}.",
            operation,
            (int)response.StatusCode);

        var errorMessage =
            $"{operation} failed with HTTP status code {(int)response.StatusCode}.";

        return CreateFailure<TResponse>(
            response.StatusCode,
            errorMessage,
            null);
    }

    /// <summary>
    /// Sends a POST request and evaluates its success using the HTTP status code
    /// without attempting to deserialize the response body.
    /// </summary>
    /// <param name="endpoint">The API endpoint to send the request to.</param>
    /// <param name="operation">A description of the operation being performed.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A response indicating whether the request was successful and the HTTP
    /// status code returned by PayChangu.
    /// </returns>
    private async Task<PayChanguApiResponse> PostStatusAsync(
        string endpoint,
        string operation,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        using var response = await SendHttpRequestAsync(
            request,
            operation,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogDebug(
                "PayChangu API operation {Operation} completed successfully with HTTP status code {StatusCode}.",
                operation,
                (int)response.StatusCode);

            return new PayChanguApiResponse
            {
                Success = true,
                StatusCode = response.StatusCode
            };
        }

        _logger.LogWarning(
            "PayChangu API operation {Operation} returned HTTP status code {StatusCode}.",
            operation,
            (int)response.StatusCode);

        return new PayChanguApiResponse
        {
            Success = false,
            StatusCode = response.StatusCode,
            ErrorMessage =
                $"{operation} failed with HTTP status code {(int)response.StatusCode}."
        };
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
    /// <param name="paymentFlow"></param>
    /// <returns>The normalized payment verification response.</returns>
    private static VerifyPaymentResponse MapApiVerificationResponse(
        ApiVerifyPaymentResponse response,
        string transactionIdentifier,
        PayChanguPaymentMethod paymentMethod,
        PayChanguPaymentFlow paymentFlow)
    {
        var data = response.Data;

        return new VerifyPaymentResponse
        {
            PaymentFlow = paymentFlow,
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
        PayChanguTransaction transaction,
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
                    Amount = transaction.TransactionCharges.Amount
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
                    PayerBankId = transaction.Authorization.PayerBankId,
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