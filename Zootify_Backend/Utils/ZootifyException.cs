using System.Text.Json.Serialization;
using Zootify_Backend.Enums;

namespace Zootify_Backend;

/// <summary>
/// Application-specific exception that carries an <see cref="ErrorCode"/> identifying the failure.
/// The human-readable message is resolved at the HTTP boundary by <c>ExceptionMiddleware</c>
/// using the caller's <c>Accept-Language</c> header.
/// </summary>
public class ZootifyException : Exception
{
    /// <summary>The error code that identifies the specific error.</summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// Initializes a <see cref="ZootifyException"/> using only an error code.
    /// The exception message is the enum member name, used for server-side logging.
    /// </summary>
    public ZootifyException(ErrorCode errorCode)
        : base(errorCode.ToString())
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a <see cref="ZootifyException"/> wrapping an inner exception.
    /// Used when a lower-level exception is the root cause.
    /// </summary>
    public ZootifyException(ErrorCode errorCode, Exception? exception)
        : base(errorCode.ToString(), exception)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a <see cref="ZootifyException"/> from a JSON-deserialized error string.
    /// Required for deserializing error responses back into this type.
    /// </summary>
    [JsonConstructor]
    public ZootifyException(ErrorCode errorCode, string exception)
        : base(errorCode.ToString())
    {
        ErrorCode = errorCode;
    }
}
