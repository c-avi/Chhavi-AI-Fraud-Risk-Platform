using System.Net;
using System.Text.Json;

namespace FraudRiskApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
            throw exception;

        var (statusCode, title, detail) = MapException(exception);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            ArgumentException or ArgumentNullException or FormatException =>
                ((int)HttpStatusCode.BadRequest, "Bad Request", exception.Message),
            UnauthorizedAccessException =>
                ((int)HttpStatusCode.Unauthorized, "Unauthorized", exception.Message),
            KeyNotFoundException =>
                ((int)HttpStatusCode.NotFound, "Not Found", exception.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error",
                "An unexpected error occurred.")
        };
}
