using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when retrieving a PayChangu wallet balance.
/// </summary>
public sealed class GetBalanceResponse
{
    /// <summary>
    /// Gets or sets the status of the API response.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the wallet balance details.
    /// </summary>
    [JsonPropertyName("data")]
    public BalanceData Data { get; set; } = null!;
}

/// <summary>
/// Represents the wallet balance details returned by PayChangu.
/// </summary>
public sealed class BalanceData
{
    /// <summary>
    /// Gets or sets the environment in which the wallet operates.
    /// </summary>
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = null!;

    /// <summary>
    /// Gets or sets the wallet currency.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the main wallet balance.
    /// </summary>
    [JsonPropertyName("main_balance")]
    public string MainBalance { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection wallet balance.
    /// </summary>
    [JsonPropertyName("collection_balance")]
    public decimal CollectionBalance { get; set; }
}

