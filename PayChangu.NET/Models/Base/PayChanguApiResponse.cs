using System.Net;

namespace PayChangu.NET.Models.Base;

/// <summary>
/// Represents the result of an API request to PayChangu.
/// </summary>
public class PayChanguApiResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the API request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code returned by the API.
    /// </summary>
    public HttpStatusCode? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error message returned when the API request fails.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the result of an API request to PayChangu that returns data.
/// </summary>
/// <typeparam name="T">The type of data returned by the API.</typeparam>
public class PayChanguApiResponse<T> : PayChanguApiResponse
{
    /// <summary>
    /// Gets or sets the data returned by the API.
    /// </summary>
    public T? Result { get; set; }


    /// <summary>
    /// Gets or sets the raw json returned by the api.
    /// </summary>
    public string? RawJsonResponse { get; set; }
}