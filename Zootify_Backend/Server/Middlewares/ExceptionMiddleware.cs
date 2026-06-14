using System.Globalization;
using System.Net;
using System.Resources;
using Newtonsoft.Json;
using Zootify_Backend.Enums;
using Zootify_Backend.Resources;

namespace Zootify_Backend.Server.Middlewares;

/// <summary>
/// Global exception middleware that catches <see cref="ZootifyException"/> instances,
/// resolves the error message in the caller's language via <see cref="ResourceManager"/>,
/// and returns the appropriate HTTP status code with a JSON error body.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly ResourceManager ResourceManager =
        new("Zootify_Backend.Resources.ErrorMessages",
            typeof(ErrorMessages).Assembly);

    /// <summary>
    /// Initializes a new <see cref="ExceptionMiddleware"/> with the next middleware delegate.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the next middleware and intercepts any <see cref="ZootifyException"/> thrown downstream.
    /// The culture is already set by <c>UseRequestLocalization</c> before this runs.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ZootifyException e)
        {
            await HandleException(context, e);
        }
    }

    /// <summary>
    /// Maps the <see cref="ZootifyException"/> to a JSON response body with a localized message
    /// and sets the HTTP status code.
    /// Infrastructure errors (DatabaseError, None, DefaultErrorCode) return 500; all others return 400.
    /// </summary>
    /// <param name="context">The current HTTP context used to write the response.</param>
    /// <param name="e">The caught <see cref="ZootifyException"/> to handle.</param>
    /// <returns>A task that completes once the error response has been written.</returns>
    private static Task HandleException(HttpContext context, ZootifyException e)
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

        var message = ResourceManager.GetString(
                          e.ErrorCode.ToString(), CultureInfo.CurrentUICulture)
                      ?? e.ErrorCode.ToString();

        var response = new
        {
            ErrorCode = e.ErrorCode,
            ErrorMessage = message,
        };

        return context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
}
