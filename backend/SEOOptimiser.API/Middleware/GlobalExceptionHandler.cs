using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SEOOptimiser.API.Middleware;

public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        LogUnhandledException(
            logger,
            exception,
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path.ToString(),
            detail);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = detail,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int statusCode, string title, string detail) MapException(Exception ex) => ex switch
    {
        KeyNotFoundException e      => (StatusCodes.Status404NotFound,            "Not Found",             e.Message),
        ArgumentException e         => (StatusCodes.Status400BadRequest,           "Bad Request",           e.Message),
        InvalidOperationException e => (StatusCodes.Status400BadRequest,           "Bad Request",           e.Message),
        OperationCanceledException  => (499,                                       "Client Closed Request", "The client closed the connection before the request completed."),
        _                           => (StatusCodes.Status500InternalServerError,  "Internal Server Error", "An unexpected error occurred."),
    };

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unhandled {ExceptionType} on {Method} {Path} — {Detail}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception,
        string exceptionType,
        string method,
        string path,
        string detail);
}
