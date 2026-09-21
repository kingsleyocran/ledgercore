using System.Text.Json;
using LedgerCore.Domain.Exceptions;

namespace LedgerCore.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (LedgerCoreException ex)
        {
            await HandleLedgerException(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions));
        }
    }

    private static async Task HandleLedgerException(HttpContext context, LedgerCoreException ex)
    {
        var statusCode = ex switch
        {
            AccountNotFoundException => 404,
            EntryNotFoundException => 404,
            DuplicateReferenceException => 409,
            _ => 400
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions));
    }
}
