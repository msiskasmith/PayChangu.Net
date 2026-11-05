using PayChangu.NET.Exceptions;
using PayChangu.NET.Models;
using System;
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
    /// A <see cref="PaymentResponse"/> containing the checkout URL and
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
    Task<PaymentResponse> InitiatePaymentAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the status of a PayChangu transaction using its transaction reference.
    /// </summary>
    /// <param name="transactionReference">
    /// The merchant-generated transaction reference supplied when initiating the payment.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The verification response containing the transaction status and details.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the transaction reference is null, empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="PayChanguException">
    /// Thrown when PayChangu returns an unsuccessful response or an invalid response payload.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    Task<CheckoutPaymentVerificationResponse> VerifyTransactionAsync(
        string transactionReference,
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
    Task<MobileMoneyChargeResponse> InitiateMobileMoneyChargeAsync(
        MobileMoneyDirectChargeRequest request,
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
    public async Task<PaymentResponse> InitiatePaymentAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "payment");

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
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
            var result = JsonSerializer.Deserialize<PaymentResponse>(
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
    public async Task<CheckoutPaymentVerificationResponse> VerifyTransactionAsync(
        string transactionReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionReference))
        {
            throw new ArgumentException(
                "Transaction reference is required.",
                nameof(transactionReference));
        }

        var endpoint = $"verify-payment/{Uri.EscapeDataString(transactionReference)}";

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
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
            var result = JsonSerializer.Deserialize<CheckoutPaymentVerificationResponse>(
                content,
                _jsonOptions);

            return result ?? throw new PayChanguException(
                "PayChangu returned an empty verification response.");
        }
        catch (JsonException ex)
        {
            throw new PayChanguException(
                "Unable to deserialize PayChangu verification response.",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<MobileMoneyChargeResponse> InitiateMobileMoneyChargeAsync(
        MobileMoneyDirectChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(
            "mobile-money/payments/initialize",
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
            var result = JsonSerializer.Deserialize<MobileMoneyChargeResponse>(
                responseContent);

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
}