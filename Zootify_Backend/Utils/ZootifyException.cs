using System.Text.Json.Serialization;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;

namespace Zootify_Backend;

public class ZootifyException : Exception
{
    // The error code that identifies the specific error
    public ErrorCode ErrorCode { get; }

    // An optional error message containing additional details about the error
    public string? ErrorMessage { get; }

    // Constructor that accepts an error code and sets the default error description
    public ZootifyException(ErrorCode errorCode)
        : base(errorCode.GetDescription())
    {
        ErrorCode = errorCode;
        ErrorMessage = errorCode.GetDescription();
    }

    // Constructor that accepts an error code and an exception, passing the exception to the base class
    public ZootifyException(ErrorCode errorCode, Exception? exception)
        : base(errorCode.GetDescription(), exception)
    {
        ErrorCode = errorCode;
        ErrorMessage = exception?.ToString();
    }
    
    // Constructor marked with JsonConstructor for deserialization from JSON
    [JsonConstructor]
    public ZootifyException(ErrorCode errorCode, string exception)
    : base(errorCode.GetDescription())
    {
        ErrorCode = errorCode;
        ErrorMessage = exception;
    }
    
}