using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.Interfaces;
using SEOOptimiser.Core.UseCases.Sessions.Queries;

namespace SEOOptimiser.Core.UseCases.Messages.Commands;

public record SendMessageCommand(Guid SessionId, string UserId, string Content)
    : IRequest<SendMessageResult>;

/// <summary>Result of sending a message — contains the agent's reply with any structured suggestions.</summary>
/// <param name="AssistantMessage">
/// The assistant's reply. Includes a <c>Suggestions</c> list when the agent recorded SEO suggestions
/// during this turn; null otherwise.
/// </param>
public record SendMessageResult(ChatMessageDto AssistantMessage);

public partial class SendMessageCommandHandler(
    IAppDbContext db,
    IAgentService agentService,
    ILogger<SendMessageCommandHandler> logger)
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    public async Task<SendMessageResult> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        LogHandlingSendMessage(logger, request.SessionId, request.UserId);

        var session = await db.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Suggestions)
            .FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.UserId == request.UserId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        var userMsg = ChatMessage.Create(session.Id, MessageRole.User, request.Content);
        db.ChatMessages.Add(userMsg);

        var priorMessages = session.Messages.ToList();

        LogCallingAgent(logger, request.SessionId, priorMessages.Count);

        AgentRunResult agentResult;
        try
        {
            agentResult = await agentService.RunAsync(priorMessages, request.Content, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAgentCallFailed(logger, ex, request.SessionId);
            throw;
        }

        LogAgentCompleted(logger, request.SessionId, agentResult.Suggestions.Count);

        var assistantMsg = ChatMessage.Create(session.Id, MessageRole.Assistant, agentResult.Text);
        db.ChatMessages.Add(assistantMsg);

        foreach (var s in agentResult.Suggestions)
            db.Suggestions.Add(Suggestion.Create(assistantMsg.Id, s.Tag, s.CurrentValue, s.SuggestedValue));

        session.UpdatedAt = DateTime.UtcNow;

        LogSavingMessages(logger, request.SessionId);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogDatabaseSaveFailed(logger, ex, request.SessionId);
            throw;
        }

        var suggestionDtos = agentResult.Suggestions
            .Select(s => new SuggestionDto(s.Tag, s.CurrentValue, s.SuggestedValue))
            .ToList();

        return new SendMessageResult(
            new ChatMessageDto(assistantMsg.Id, "Assistant", agentResult.Text, assistantMsg.CreatedAt, suggestionDtos));
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Handling SendMessage for session {SessionId}, user {UserId}")]
    private static partial void LogHandlingSendMessage(ILogger logger, Guid sessionId, string userId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Calling agent for session {SessionId} ({PriorCount} prior messages)")]
    private static partial void LogCallingAgent(ILogger logger, Guid sessionId, int priorCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Agent call failed for session {SessionId}")]
    private static partial void LogAgentCallFailed(ILogger logger, Exception ex, Guid sessionId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Agent completed for session {SessionId}: {SuggestionCount} suggestions")]
    private static partial void LogAgentCompleted(ILogger logger, Guid sessionId, int suggestionCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saving messages for session {SessionId}")]
    private static partial void LogSavingMessages(ILogger logger, Guid sessionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Database save failed for session {SessionId}")]
    private static partial void LogDatabaseSaveFailed(ILogger logger, Exception ex, Guid sessionId);
}
