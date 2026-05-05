using Microsoft.Extensions.Http;

namespace SEOOptimiser.Eval.Infrastructure;

// Satisfies SeoAgentService's constructor requirement. Never actually called because
// fakePageHtml is always set in eval mode, so the real HTTP path is never reached.
public sealed class NullHttpClientFactory : IHttpClientFactory
{
    public static readonly NullHttpClientFactory Instance = new();

    public HttpClient CreateClient(string name) =>
        throw new InvalidOperationException(
            "NullHttpClientFactory: real HTTP calls are disabled in eval mode. " +
            "Ensure fakePageHtml is always set when constructing SeoAgentService for eval.");
}
