using System.Net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Zootify_Backend.Enums;
using Zootify_Backend.Resources;

namespace Zootify_Backend.Server.Middlewares;

/// <summary>
/// Global exception middleware that catches <see cref="ZootifyException"/> instances,
/// resolves the error message in the caller's language via <see cref="IStringLocalizer{T}"/>,
/// and returns the appropriate HTTP status code with a JSON error body.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new <see cref="ExceptionMiddleware"/> with the next middleware delegate.
    /// </summary>
    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the next middleware and intercepts any <see cref="ZootifyException"/> thrown downstream.
    /// The <paramref name="localizer"/> is resolved per-request so it picks up the culture
    /// already set by <c>UseRequestLocalization</c>.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IStringLocalizer<ErrorMessages> localizer)
    {
        try
        {
            await _next(context);
        }
        catch (ZootifyException e)
        {
            await HandleException(context, e, localizer);
        }
    }

    /// <summary>
    /// Maps the <see cref="ZootifyException"/> to a JSON response body with a localized message
    /// and sets the HTTP status code.
    /// Infrastructure errors (DatabaseError, None, DefaultErrorCode) return 500; all others return 400.
    /// </summary>
    private static Task HandleException(
        HttpContext context,
        ZootifyException e,
        IStringLocalizer<ErrorMessages> localizer)
    {
        context.Response.ContentType = "application/json";

        switch (e.ErrorCode)
        {
            case ErrorCode.DatabaseError:
            case ErrorCode.None:
            case ErrorCode.DefaultErrorCode:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
        }

        var response = new
        {
            ErrorCode = e.ErrorCode,
            ErrorMessage = localizer[e.ErrorCode.ToString()].Value,
        };

        return context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
}
