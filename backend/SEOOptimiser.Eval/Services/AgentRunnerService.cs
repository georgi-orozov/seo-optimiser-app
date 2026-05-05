using Microsoft.Extensions.Logging.Abstractions;
using SEOOptimiser.Eval.Infrastructure;
using SEOOptimiser.Eval.Models;
using SEOOptimiser.Infrastructure.Services;
using CoreChatMessage = SEOOptimiser.Core.Entities.ChatMessage;

namespace SEOOptimiser.Eval.Services;

public sealed class AgentRunnerService
{
    private readonly string _anthropicApiKey;

    public AgentRunnerService(string anthropicApiKey)
    {
        _anthropicApiKey = anthropicApiKey;
    }

    public async Task<AgentOutput> RunAsync(TestCase testCase, CancellationToken ct = default)
    {
        // Create a fresh agent instance per test case with the test HTML injected.
        // NullHttpClientFactory is safe here because fakePageHtml is always set,
        // so the real HTTP code path in SeoAgentService is never reached.
        var agent = new SeoAgentService(
            httpClientFactory: NullHttpClientFactory.Instance,
            logger: NullLogger<SeoAgentService>.Instance,
            anthropicApiKey: _anthropicApiKey,
            fakePageHtml: testCase.FakePageHtml);

        // Include keywords in the opening message so the agent skips its
        // clarifying-question step and goes straight to generating suggestions.
        var message = testCase.TargetKeywords.Length > 0
            ? $"{testCase.UserMessage} My target keywords are: {string.Join(", ", testCase.TargetKeywords)}."
            : testCase.UserMessage;

        var result = await agent.RunAsync(
            priorMessages: Array.Empty<CoreChatMessage>(),
            userMessage: message,
            cancellationToken: ct);

        var suggestions = result.Suggestions
            .Select(s => new SuggestionRecord(s.Tag, s.CurrentValue, s.SuggestedValue))
            .ToList();

        return new AgentOutput(result.Text, suggestions);
    }
}
