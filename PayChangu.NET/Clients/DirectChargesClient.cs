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
    /// <summary>Initiates a Mobile Money Direct Charge request.</summary>
    Task<PayChanguApiResponse<InitiateMobileMoneyDirectChargeResponse>> InitiateAsync(
        InitiateMobileMoneyDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves Mobile Money Direct Charge transaction details.</summary>
    Task<PayChanguApiResponse<GetMobileMoneyChargeDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides Bank Transfer Direct Charge operations.</summary>
public interface IPayChanguBankTransferDirectChargeClient
{
    /// <summary>Initiates a Bank Transfer Direct Charge request.</summary>
    Task<PayChanguApiResponse<InitiateBankTransferDirectChargeResponse>> InitiateAsync(
        InitiateBankTransferDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves Bank Transfer Direct Charge transaction details.</summary>
    Task<PayChanguApiResponse<GetBankTransferDirectChargeDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides Card Direct Charge operations.</summary>
public interface IPayChanguCardDirectChargeClient
{
    /// <summary>Initiates a Card Direct Charge request.</summary>
    Task<PayChanguApiResponse<InitiateCardDirectChargeResponse>> InitiateAsync(
        InitiateCardDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refunds a Card Direct Charge transaction.</summary>
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
