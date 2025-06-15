using System.Net;
using System.Security.Claims;
using Currency.Api.Models;
using Currency.Api.Schemes;
using Currency.Facades.Contracts.Exceptions;
using Serilog.Context;

namespace Currency.Api.Middlewares;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            await HandleValidationException(context, validationException);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private async Task HandleValidationException(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await context.Response.WriteAsJsonAsync(new ErrorResponseScheme
        {
            Error = ErrorMessage.ValidationError,
            Message = ex.Message,
            Details = ex.ErrorMessages
        });
    }

    private async Task HandleException(HttpContext context, Exception ex)
    {
        // Push same properties for context consistency in exception logs
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "undefined";
        var requestId = context.TraceIdentifier;
        var clientId = context.User?.Claims.FirstOrDefault(c => 
            c.Type == ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        using (LogContext.PushProperty("ClientIP", clientIp))
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("ClientId", clientId))
        {
            _logger.LogError(ex, "Unexpected error occurred: {Message}. TraceId: {TraceId}", ex.Message, requestId);
        }

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsJsonAsync(new ErrorResponseScheme
        {
            Error = ErrorMessage.InternalServerError,
            Message = "An unexpected error occurred while processing your request.",
            Details = new
            {
                TraceId = requestId,
                Message = ex.Message,
                StackTract = ex.StackTrace,
            }
        });
    }
}
