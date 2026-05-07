using System.Diagnostics;

namespace SEOOptimiser.Infrastructure.Telemetry;

public static class SeoTelemetry
{
    public const string SourceName = "SEOOptimiser.Agent";
    public static readonly ActivitySource Source = new(SourceName);
}
