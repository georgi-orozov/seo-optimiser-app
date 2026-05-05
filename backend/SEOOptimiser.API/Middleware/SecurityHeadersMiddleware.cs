namespace SEOOptimiser.API.Middleware;

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"]        = "DENY";
            ctx.Response.Headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
            ctx.Response.Headers["Permissions-Policy"]     = "camera=(), microphone=(), geolocation=()";
            await next(ctx);
        });
}
