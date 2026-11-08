using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when retrieving supported payout banks.
/// </summary>
public sealed class GetSupportedBanksResponse
{
    /// <summary>
    /// Gets or sets the API response status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of supported banks and payout providers.
    /// </summary>
    [JsonPropertyName("data")]
    public List<SupportedBank> Data { get; set; } = new();
}

/// <summary>
/// Represents a bank or payout provider supported by PayChangu.
/// </summary>
public sealed class SupportedBank
{
    /// <summary>
    /// Gets or sets the provider UUID.
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}