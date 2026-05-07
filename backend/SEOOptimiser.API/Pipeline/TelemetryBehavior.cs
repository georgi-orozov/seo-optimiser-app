using System.Diagnostics;
using MediatR;
using SEOOptimiser.Infrastructure.Telemetry;

namespace SEOOptimiser.API.Pipeline;

public sealed class TelemetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = SeoTelemetry.Source.StartActivity(
            $"mediator.{requestName}", ActivityKind.Internal);
        activity?.SetTag("mediator.request", requestName);

        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
