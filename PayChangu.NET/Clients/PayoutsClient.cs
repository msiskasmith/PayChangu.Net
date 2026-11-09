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
    /// <summary>Initiates a Mobile Money payout.</summary>
    Task<PayChanguApiResponse<InitiateMobileMoneyPayoutResponse>> InitiateAsync(
        InitiateMobileMoneyPayoutRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides bank payout operations.</summary>
public interface IPayChanguBankPayoutClient
{
    /// <summary>Initiates a bank payout.</summary>
    Task<PayChanguApiResponse<InitiateBankPayoutResponse>> InitiateAsync(
        InitiateBankPayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the details of a bank payout.</summary>
    Task<PayChanguApiResponse<GetSingleBankPayoutDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the available page of bank payout transactions.</summary>
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
