using System.Text.Json;

namespace SupportAgent.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static async Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        var (status, message) = ex switch
        {
            KeyNotFoundException      => (404, ex.Message),
            InvalidOperationException => (409, ex.Message),
            ArgumentException         => (400, ex.Message),
            UnauthorizedAccessException => (403, "Access denied."),
            _                         => (500, "An unexpected error occurred.")
        };

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
