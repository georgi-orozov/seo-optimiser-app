using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SEOOptimiser.API.Middleware;

namespace SEOOptimiser.API.Tests.Middleware;

public class GlobalExceptionHandlerTest
{
    private static GlobalExceptionHandler CreateHandler() =>
        new(new Mock<ILogger<GlobalExceptionHandler>>().Object);

    private static DefaultHttpContext CreateHttpContext(string traceId = "trace-abc")
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        ctx.TraceIdentifier = traceId;
        return ctx;
    }

    private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext ctx)
    {
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<ProblemDetails>(
            ctx.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_Returns404()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new KeyNotFoundException("not found"), CancellationToken.None);

        ctx.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentException_Returns400()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new ArgumentException("bad arg"), CancellationToken.None);

        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task TryHandleAsync_WithInvalidOperationException_Returns400()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new InvalidOperationException("invalid"), CancellationToken.None);

        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task TryHandleAsync_WithOperationCanceledException_Returns499()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new OperationCanceledException(), CancellationToken.None);

        ctx.Response.StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnknownException_Returns500()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new Exception("boom"), CancellationToken.None);

        ctx.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TryHandleAsync_WithAnyException_ReturnsTrue()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        var handled = await handler.TryHandleAsync(ctx, new Exception("boom"), CancellationToken.None);

        handled.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_WithAnyException_IncludesTraceIdInResponse()
    {
        var ctx = CreateHttpContext("my-trace-id");
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new Exception("boom"), CancellationToken.None);

        var problem = await ReadProblemDetailsAsync(ctx);
        problem!.Extensions.Should().ContainKey("traceId");
        problem.Extensions["traceId"]!.ToString().Should().Be("my-trace-id");
    }

    [Fact]
    public async Task TryHandleAsync_WithKeyNotFoundException_IncludesNotFoundTitle()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new KeyNotFoundException("Session not found"), CancellationToken.None);

        var problem = await ReadProblemDetailsAsync(ctx);
        problem!.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task TryHandleAsync_WithUnknownException_IncludesInternalServerErrorTitle()
    {
        var ctx = CreateHttpContext();
        var handler = CreateHandler();

        await handler.TryHandleAsync(ctx, new Exception("boom"), CancellationToken.None);

        var problem = await ReadProblemDetailsAsync(ctx);
        problem!.Title.Should().Be("Internal Server Error");
    }
}
