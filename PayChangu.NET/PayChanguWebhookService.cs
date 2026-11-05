using PayChangu.NET.Models;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PayChangu.NET;

/// <summary>
/// Defines methods for validating and parsing PayChangu webhook notifications.
/// </summary>
public interface IPayChanguWebhookService
{
    /// <summary>
    /// Validates the HMAC-SHA256 signature of a webhook payload using the configured
    /// PayChangu webhook secret.
    /// </summary>
    /// <param name="payload">
    /// The raw webhook request body exactly as received from PayChangu.
    /// </param>
    /// <param name="signature">
    /// The signature supplied in the webhook request's <c>Signature</c> header.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the signature is valid; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    bool VerifySignature(
        string payload,
        string signature);

    /// <summary>
    /// Deserializes a webhook payload into a strongly typed
    /// <see cref="PayChanguWebhookEvent"/>.
    /// </summary>
    /// <param name="payload">
    /// The raw JSON webhook request body.
    /// </param>
    /// <returns>
    /// The parsed webhook event.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The payload is null, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="System.Text.Json.JsonException">
    /// The payload is not valid JSON or cannot be deserialized into a webhook event.
    /// </exception>
    PayChanguWebhookEvent ParseWebhook(
        string payload);
}

/// <summary>
/// Provides functionality for validating and parsing PayChangu webhook events.
/// </summary>
internal sealed class PayChanguWebhookService : IPayChanguWebhookService
{
    private readonly byte[] _webhookSecret;

    public PayChanguWebhookService(string? webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {

            throw new ArgumentException(
                "PayChangu WebhookSecret is required.",
                nameof(webhookSecret));
        }

        _webhookSecret = Encoding.UTF8.GetBytes(webhookSecret);
    }

    /// <inheritdoc />
    public bool VerifySignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(payload) ||
            string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        // A SHA256 hexadecimal signature must contain 64 characters.
        if (signature.Length != 64)
        {
            return false;
        }

        using var hmac = new HMACSHA256(_webhookSecret);

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var computedHash = hmac.ComputeHash(payloadBytes);

        if (!TryConvertHexToBytes(signature, out var receivedHash))
        {
            return false;
        }

        return FixedTimeEquals(computedHash, receivedHash);
    }

    private static bool TryConvertHexToBytes(
        string hex,
        out byte[] bytes)
    {
        bytes = new byte[hex.Length / 2];

        for (var i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(
                hex.Substring(i * 2, 2),
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out bytes[i]))
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }

        return true;
    }

    private static bool FixedTimeEquals(byte[] expected, byte[] actual)
    {
        if (expected.Length != actual.Length)
        {
            return false;
        }

        var difference = 0;

        for (var i = 0; i < expected.Length; i++)
        {
            difference |= expected[i] ^ actual[i];
        }

        return difference == 0;
    }

    /// <inheritdoc />
    public PayChanguWebhookEvent ParseWebhook(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(
                "Payload cannot be null or empty.",
                nameof(payload));
        }
        try
        {
            var webhookEvent = JsonSerializer.Deserialize<PayChanguWebhookEvent>(payload);
            if (webhookEvent == null)
            {
                throw new JsonException("Failed to deserialize the webhook payload.");
            }
            return webhookEvent;
        }
        catch (JsonException ex)
        {
            throw new JsonException(
                "Failed to parse the webhook payload. Ensure it is valid JSON.",
                ex);
        }
    }       
}
