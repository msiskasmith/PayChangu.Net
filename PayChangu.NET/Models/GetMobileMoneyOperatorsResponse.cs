
using PayChangu.NET.Models.Base;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when retrieving supported mobile money operators.
/// </summary>
public sealed class GetMobileMoneyOperatorsResponse : BasePaychanguResponse
{
    /// <summary>
    /// Gets or sets the supported mobile money operators.
    /// </summary>
    [JsonPropertyName("data")]
    public List<SupportedMobileMoneyOperator> Data { get; set; } = new();
}

/// <summary>
/// Represents a mobile money operator supported by PayChangu.
/// </summary>
public sealed class SupportedMobileMoneyOperator
{
    /// <summary>
    /// Gets or sets the operator identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the operator name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the operator reference identifier.
    /// </summary>
    [JsonPropertyName("ref_id")]
    public string RefId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the operator short code.
    /// </summary>
    [JsonPropertyName("short_code")]
    public string ShortCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the operator logo URL.
    /// </summary>
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operator supports withdrawals.
    /// </summary>
    [JsonPropertyName("supports_withdrawals")]
    public bool SupportsWithdrawals { get; set; }

    /// <summary>
    /// Gets or sets the country supported by the operator.
    /// </summary>
    [JsonPropertyName("supported_country")]
    public SupportedCountry SupportedCountry { get; set; } = null!;
}

/// <summary>
/// Represents a country supported by a mobile money operator.
/// </summary>
public sealed class SupportedCountry
{
    /// <summary>
    /// Gets or sets the country name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the country's currency code.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;
}


