using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Core.Interfaces;

/// <summary>A raw suggestion captured from an agent tool call, before persistence.</summary>
public record SuggestionCapture(string Tag, string? CurrentValue, string SuggestedValue);

/// <summary>The result of one agent turn: natural language reply plus any suggestions captured via tool calls.</summary>
public record AgentRunResult(string Text, IReadOnlyList<SuggestionCapture> Suggestions);

public interface IAgentService
{
    /// <summary>
    /// Runs one agent turn. Prior conversation messages are loaded from <paramref name="priorMessages"/>
    /// so the agent maintains context. Returns the agent's text reply and any SEO suggestions
    /// captured via the <c>record_seo_suggestion</c> tool during this turn.
    /// </summary>
    Task<AgentRunResult> RunAsync(
        IReadOnlyList<ChatMessage> priorMessages,
        string userMessage,
        CancellationToken cancellationToken = default);
}
