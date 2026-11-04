using System;
using System.Net;

namespace PayChangu.NET.Exceptions;

/// <summary>
/// Represents an error that occurs while communicating with the PayChangu API
/// or processing its response.
/// </summary>
/// <remarks>
/// This exception provides additional information about API failures,
/// including the HTTP status code and response content when available.
/// </remarks>
public sealed class PayChanguException : Exception
{
    /// <summary>
    /// Gets the HTTP status code returned by PayChangu, if available.
    /// </summary>
    /// <remarks>
    /// This property is populated when the exception is caused by an
    /// unsuccessful HTTP response.
    /// </remarks>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the raw response content returned by PayChangu, if available.
    /// </summary>
    /// <remarks>
    /// This can contain additional error details provided by the API.
    /// </remarks>
    public string? ResponseContent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayChanguException"/> class
    /// with the specified error message.
    /// </summary>
    /// <param name="message">A message describing the error.</param>
    public PayChanguException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayChanguException"/> class
    /// with an error message, HTTP status code, and response content.
    /// </summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="statusCode">The HTTP status code returned by PayChangu.</param>
    /// <param name="responseContent">The raw response content returned by PayChangu.</param>
    public PayChanguException(
        string message,
        HttpStatusCode statusCode,
        string responseContent)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayChanguException"/> class
    /// with the specified error message and underlying exception.
    /// </summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public PayChanguException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}