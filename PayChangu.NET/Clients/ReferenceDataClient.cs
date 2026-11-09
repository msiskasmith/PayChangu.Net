using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>Provides access to PayChangu reference data.</summary>
public interface IPayChanguReferenceDataClient
{
    /// <summary>Gets the supported Mobile Money operators.</summary>
    IPayChanguMobileMoneyOperatorsClient MobileMoneyOperators { get; }

    /// <summary>Gets the supported banks and payout providers.</summary>
    IPayChanguBanksClient Banks { get; }
}

/// <summary>Provides Mobile Money operator reference data operations.</summary>
public interface IPayChanguMobileMoneyOperatorsClient
{
    /// <summary>
    /// Retrieves the mobile money operators supported by PayChangu.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the supported mobile money operators, or failure
    /// details if the request was unsuccessful.
    /// </returns>
    Task<PayChanguApiResponse<GetMobileMoneyOperatorsResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>Provides bank and payout provider reference data operations.</summary>
public interface IPayChanguBanksClient
{
    /// <summary>
    /// Retrieves the banks and payout providers supported by PayChangu
    /// for the specified currency.
    /// </summary>
    /// <param name="currency">
    /// The currency code used to filter supported providers. Defaults to <c>MWK</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A response containing the supported banks and payout providers,
    /// or failure details if the request was unsuccessful.
    /// </returns>
    Task<PayChanguApiResponse<GetSupportedBanksResponse>> GetSupportedAsync(
        PayChanguCurrency currency = PayChanguCurrency.MWK,
        CancellationToken cancellationToken = default);
}

internal sealed class PayChanguReferenceDataClient : IPayChanguReferenceDataClient
{
    public PayChanguReferenceDataClient(PayChanguClient client)
    {
        MobileMoneyOperators = new PayChanguMobileMoneyOperatorsClient(client);
        Banks = new PayChanguBanksClient(client);
    }
    public IPayChanguMobileMoneyOperatorsClient MobileMoneyOperators { get; }
    public IPayChanguBanksClient Banks { get; }
}

internal sealed class PayChanguMobileMoneyOperatorsClient : IPayChanguMobileMoneyOperatorsClient
{
    private readonly PayChanguClient _client;
    public PayChanguMobileMoneyOperatorsClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<GetMobileMoneyOperatorsResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _client.GetMobileMoneyOperatorsAsync(cancellationToken);
}

internal sealed class PayChanguBanksClient : IPayChanguBanksClient
{
    private readonly PayChanguClient _client;
    public PayChanguBanksClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<GetSupportedBanksResponse>> GetSupportedAsync(
        PayChanguCurrency currency = PayChanguCurrency.MWK,
        CancellationToken cancellationToken = default) =>
        _client.GetSupportedBanksAsync(currency, cancellationToken);
}
