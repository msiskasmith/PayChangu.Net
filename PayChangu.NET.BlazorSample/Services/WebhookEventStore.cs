using System.Collections.Concurrent;
using PayChangu.NET.Models;

namespace PayChangu.NET.BlazorSample.Services;

/// <summary>
/// Stores recently received PayChangu webhook events for the sample application.
/// </summary>
public sealed class WebhookEventStore
{
    private const int MaximumEventCount = 100;

    private readonly ConcurrentQueue<WebhookEventLogEntry> _events = new();

    /// <summary>
    /// Adds a successfully parsed webhook event and its payment verification result
    /// to the in-memory event history.
    /// </summary>
    public void Add(
        PayChanguWebhookEvent webhookEvent,
        bool? paymentVerified = null,
        string? verificationStatus = null,
        string? verificationMessage = null)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        _events.Enqueue(new WebhookEventLogEntry(
            DateTimeOffset.UtcNow,
            webhookEvent.EventType,
            webhookEvent.PaymentFlow,
            webhookEvent.PaymentMethod,
            webhookEvent.TransactionIdentifier,
            webhookEvent.Status,
            webhookEvent.Amount,
            webhookEvent.Currency,
            webhookEvent.Mode,
            paymentVerified,
            verificationStatus,
            verificationMessage)
        {
            WebhookEvent = webhookEvent
        });

        while (_events.Count > MaximumEventCount)
        {
            _events.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Gets a stored webhook event by its sample application identifier.
    /// </summary>
    public WebhookEventLogEntry? GetEvent(Guid id)
    {
        return _events.FirstOrDefault(webhookEvent => webhookEvent.Id == id);
    }

    /// <summary>
    /// Gets the stored webhook events, newest first.
    /// </summary>
    public IReadOnlyList<WebhookEventLogEntry> GetRecentEvents()
    {
        return _events.ToArray()
            .Reverse()
            .ToArray();
    }

    /// <summary>
    /// Removes all stored webhook events.
    /// </summary>
    public void Clear()
    {
        while (_events.TryDequeue(out _))
        {
        }
    }
}

/// <summary>
/// Represents a webhook event entry displayed in the sample application.
/// </summary>
public sealed record WebhookEventLogEntry(
    DateTimeOffset ReceivedAt,
    string EventType,
    PayChanguPaymentFlow PaymentFlow,
    PayChanguPaymentMethod PaymentMethod,
    string TransactionIdentifier,
    string Status,
    decimal Amount,
    string Currency,
    string Mode,
    bool? PaymentVerified,
    string? VerificationStatus,
    string? VerificationMessage)
{
    /// <summary>
    /// Gets the unique identifier assigned by the sample application.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the original webhook event payload.
    /// </summary>
    public PayChanguWebhookEvent WebhookEvent { get; init; } = null!;
}
