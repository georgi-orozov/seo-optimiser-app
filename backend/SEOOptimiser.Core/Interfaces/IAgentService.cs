using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Core.Interfaces;

public interface IAgentService
{
    /// <summary>
    /// Runs one agent turn. Prior conversation messages are loaded from <paramref name="priorMessages"/>
    /// so the agent maintains context. Returns the agent's text reply.
    /// </summary>
    Task<string> RunAsync(
        string sessionId,
        IReadOnlyList<ChatMessage> priorMessages,
        string userMessage,
        CancellationToken cancellationToken = default);
}
