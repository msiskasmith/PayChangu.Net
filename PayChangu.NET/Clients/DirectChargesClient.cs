using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>
/// Provides Direct Charge operations grouped by payment method.
/// </summary>
public interface IPayChanguDirectChargesClient
{
    /// <summary>Gets Mobile Money Direct Charge operations.</summary>
    IPayChanguMobileMoneyDirectChargeClient MobileMoney { get; }

    /// <summary>Gets Bank Transfer Direct Charge operations.</summary>
    IPayChanguBankTransferDirectChargeClient BankTransfer { get; }

    /// <summary>Gets Card Direct Charge operations.</summary>
    IPayChanguCardDirectChargeClient Card { get; }
}

/// <summary>Provides Mobile Money Direct Charge operations.</summary>
public interface IPayChanguMobileMoneyDirectChargeClient
{
    /// <summary>
    /// Initiates a Mobile Money Direct Charge payment request.
    /// </summary>
    /// <remarks>
    /// A successful initiation response indicates that PayChangu accepted
    /// the charge request. It does not confirm that the customer completed
    /// the payment.
    /// </remarks>
    /// <param name="request">The mobile money charge details.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the initiated charge details, or failure
    /// details if the request was unsuccessful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<InitiateMobileMoneyDirectChargeResponse>> InitiateAsync(
        InitiateMobileMoneyDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the details of a Mobile Money Direct Charge transaction.
    /// </summary>
    /// <param name="chargeId">The PayChangu charge identifier.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the transaction details, or failure details
    /// if the request was unsuccessful.
    /// </returns>
    /// <exception cref="PayChanguException">
    /// Thrown when the charge ID is null, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<GetMobileMoneyChargeDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides Bank Transfer Direct Charge operations.</summary>
public interface IPayChanguBankTransferDirectChargeClient
{
    /// <summary>
    /// Initiates a Bank Transfer Direct Charge payment request.
    /// </summary>
    /// <param name="request">The bank transfer charge details.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the bank account details and transaction
    /// information required to complete the payment, or failure details
    /// if the request was unsuccessful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<InitiateBankTransferDirectChargeResponse>> InitiateAsync(
        InitiateBankTransferDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the details of a Bank Transfer Direct Charge transaction.
    /// </summary>
    /// <param name="chargeId">The PayChangu charge identifier.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the transaction details, or failure details
    /// if the request was unsuccessful.
    /// </returns>
    /// <exception cref="PayChanguException">
    /// Thrown when the charge ID is null, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<GetBankTransferDirectChargeDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides Card Direct Charge operations.</summary>
public interface IPayChanguCardDirectChargeClient
{
    /// <summary>
    /// Initiates a Card Direct Charge payment request.
    /// </summary>
    /// <param name="request">The card charge details.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the initiated card charge details and
    /// information about any required 3-D Secure authentication,
    /// or failure details if the request was unsuccessful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<InitiateCardDirectChargeResponse>> InitiateAsync(
        InitiateCardDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a Card Direct Charge transaction.
    /// </summary>
    /// <remarks>
    /// The response indicates success only when PayChangu returns HTTP 200.
    /// The response body is not deserialized.
    /// </remarks>
    /// <param name="chargeId">The PayChangu charge identifier of the transaction to refund.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response indicating whether the refund request was successful.
    /// </returns>
    /// <exception cref="PayChanguException">
    /// Thrown when the charge ID is null, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse> RefundAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

internal sealed class PayChanguDirectChargesClient : IPayChanguDirectChargesClient
{
    public PayChanguDirectChargesClient(PayChanguClient client)
    {
        MobileMoney = new PayChanguMobileMoneyDirectChargeClient(client);
        BankTransfer = new PayChanguBankTransferDirectChargeClient(client);
        Card = new PayChanguCardDirectChargeClient(client);
    }

    public IPayChanguMobileMoneyDirectChargeClient MobileMoney { get; }
    public IPayChanguBankTransferDirectChargeClient BankTransfer { get; }
    public IPayChanguCardDirectChargeClient Card { get; }
}

internal sealed class PayChanguMobileMoneyDirectChargeClient : IPayChanguMobileMoneyDirectChargeClient
{
    private readonly PayChanguClient _client;
    public PayChanguMobileMoneyDirectChargeClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<InitiateMobileMoneyDirectChargeResponse>> InitiateAsync(
        InitiateMobileMoneyDirectChargeRequest request, CancellationToken cancellationToken = default) =>
        _client.InitiateMobileMoneyDirectChargeAsync(request, cancellationToken);
    public Task<PayChanguApiResponse<GetMobileMoneyChargeDetailsResponse>> GetDetailsAsync(
        string chargeId, CancellationToken cancellationToken = default) =>
        _client.GetSingleMobileMoneyChargeDetailsAsync(chargeId, cancellationToken);
}

internal sealed class PayChanguBankTransferDirectChargeClient : IPayChanguBankTransferDirectChargeClient
{
    private readonly PayChanguClient _client;
    public PayChanguBankTransferDirectChargeClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<InitiateBankTransferDirectChargeResponse>> InitiateAsync(
        InitiateBankTransferDirectChargeRequest request, CancellationToken cancellationToken = default) =>
        _client.InitiateBankTransferDirectChargeAsync(request, cancellationToken);
    public Task<PayChanguApiResponse<GetBankTransferDirectChargeDetailsResponse>> GetDetailsAsync(
        string chargeId, CancellationToken cancellationToken = default) =>
        _client.GetSingleBankTransferDirectChargeDetailsAsync(chargeId, cancellationToken);
}

internal sealed class PayChanguCardDirectChargeClient : IPayChanguCardDirectChargeClient
{
    private readonly PayChanguClient _client;
    public PayChanguCardDirectChargeClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<InitiateCardDirectChargeResponse>> InitiateAsync(
        InitiateCardDirectChargeRequest request, CancellationToken cancellationToken = default) =>
        _client.InitiateCardChargeAsync(request, cancellationToken);
    public Task<PayChanguApiResponse> RefundAsync(
        string chargeId, CancellationToken cancellationToken = default) =>
        _client.RefundCardChargeAsync(chargeId, cancellationToken);
}
