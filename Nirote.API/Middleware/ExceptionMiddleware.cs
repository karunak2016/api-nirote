using System.Net;
using System.Text.Json;
using Azure;
using Microsoft.Data.SqlClient;

namespace Nirote.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (KeyNotFoundException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            _logger.LogWarning(ex, "Unique constraint violation");
            string field;
            if (ex.Message.Contains("Email", StringComparison.OrdinalIgnoreCase))
                field = "Email";
            else if (ex.Message.Contains("Phone", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("UQ_Users_Phone", StringComparison.OrdinalIgnoreCase))
                field = "Mobile number";
            else
                field = "Value";
            await WriteErrorAsync(context, HttpStatusCode.Conflict, $"{field} is already in use by another account.");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error");
            await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "Database unavailable. Please try again later.");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Storage error");
            await WriteErrorAsync(context, HttpStatusCode.BadGateway, "Storage service error. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var body = JsonSerializer.Serialize(new { error = message });
        return context.Response.WriteAsync(body);
    }
}
