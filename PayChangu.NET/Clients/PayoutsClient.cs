using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>
/// Provides payout operations grouped by payout method.
/// </summary>
public interface IPayChanguPayoutsClient
{
    /// <summary>Gets Mobile Money payout operations.</summary>
    IPayChanguMobileMoneyPayoutClient MobileMoney { get; }

    /// <summary>Gets bank payout operations.</summary>
    IPayChanguBankPayoutClient Bank { get; }
}

/// <summary>Provides Mobile Money payout operations.</summary>
public interface IPayChanguMobileMoneyPayoutClient
{
    /// <summary>
    /// Initiates a Mobile Money payout.
    /// </summary>
    /// <remarks>
    /// A successful initiation response indicates that PayChangu accepted
    /// the payout request. It does not confirm that the payout was completed.
    /// The <c>TransactionStatus</c> request property is intended exclusively
    /// for sandbox testing and must be omitted in production.
    /// </remarks>
    /// <param name="request">
    /// The payout details, including the recipient's mobile number,
    /// mobile money operator, amount, and charge identifier.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the initiated payout details, or failure
    /// details if the request was unsuccessful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<InitiateMobileMoneyPayoutResponse>> InitiateAsync(
        InitiateMobileMoneyPayoutRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides bank payout operations.</summary>
public interface IPayChanguBankPayoutClient
{
    /// <summary>
    /// Initiates a bank payout request.
    /// </summary>
    /// <remarks>
    /// A successful initiation response indicates that PayChangu accepted
    /// the payout request. It does not confirm that the payout was completed.
    /// </remarks>
    /// <param name="request">The bank payout details.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the initiated payout details, or failure
    /// details if the request was unsuccessful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<InitiateBankPayoutResponse>> InitiateAsync(
        InitiateBankPayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the details of a bank payout transaction.
    /// </summary>
    /// <param name="chargeId">The PayChangu charge identifier.</param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the bank payout transaction details, or failure
    /// details if the request was unsuccessful.
    /// </returns>
    /// <exception cref="PayChanguException">
    /// Thrown when the charge ID is null, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<GetSingleBankPayoutDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves bank payout transactions.
    /// </summary>
    /// <remarks>
    /// The response is paginated. This method retrieves the page returned
    /// by the API and does not automatically fetch subsequent pages.
    /// </remarks>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the paginated bank payout transactions,
    /// or failure details if the request was unsuccessful.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<GetAllBankPayoutDetailsResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

internal sealed class PayChanguPayoutsClient : IPayChanguPayoutsClient
{
    public PayChanguPayoutsClient(PayChanguClient client)
    {
        MobileMoney = new PayChanguMobileMoneyPayoutClient(client);
        Bank = new PayChanguBankPayoutClient(client);
    }
    public IPayChanguMobileMoneyPayoutClient MobileMoney { get; }
    public IPayChanguBankPayoutClient Bank { get; }
}

internal sealed class PayChanguMobileMoneyPayoutClient : IPayChanguMobileMoneyPayoutClient
{
    private readonly PayChanguClient _client;
    public PayChanguMobileMoneyPayoutClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<InitiateMobileMoneyPayoutResponse>> InitiateAsync(
        InitiateMobileMoneyPayoutRequest request, CancellationToken cancellationToken = default) =>
        _client.InitiateMobileMoneyPayoutAsync(request, cancellationToken);
}

internal sealed class PayChanguBankPayoutClient : IPayChanguBankPayoutClient
{
    private readonly PayChanguClient _client;
    public PayChanguBankPayoutClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<InitiateBankPayoutResponse>> InitiateAsync(
        InitiateBankPayoutRequest request, CancellationToken cancellationToken = default) =>
        _client.InitiateBankPayoutAsync(request, cancellationToken);
    public Task<PayChanguApiResponse<GetSingleBankPayoutDetailsResponse>> GetDetailsAsync(
        string chargeId, CancellationToken cancellationToken = default) =>
        _client.GetSingleBankPayoutDetailsAsync(chargeId, cancellationToken);
    public Task<PayChanguApiResponse<GetAllBankPayoutDetailsResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _client.GetAllBankPayoutDetailsAsync(cancellationToken);
}
