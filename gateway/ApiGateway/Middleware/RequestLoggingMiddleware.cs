namespace ApiGateway.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        context.Response.Headers["X-Request-Id"] = requestId;

        _logger.LogInformation(
            "→ [{RequestId}] {Method} {Path}",
            requestId, context.Request.Method, context.Request.Path);

        await _next(context);

        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
        _logger.LogInformation(
            "← [{RequestId}] {StatusCode} ({Elapsed:F1}ms)",
            requestId, context.Response.StatusCode, elapsed);
    }
}
