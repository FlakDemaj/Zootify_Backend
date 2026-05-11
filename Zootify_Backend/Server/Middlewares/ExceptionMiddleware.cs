using System.Net;
using Newtonsoft.Json;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Server.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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

        var response = new
        {
            ErrorCode = e.ErrorCode,
            ErrorMessage = e.Message,
        };

        return context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
}