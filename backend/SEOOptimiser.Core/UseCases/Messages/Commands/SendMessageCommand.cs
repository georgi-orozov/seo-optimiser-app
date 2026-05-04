using MediatR;
using Microsoft.EntityFrameworkCore;
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

public class SendMessageCommandHandler(IAppDbContext db, IAgentService agentService)
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    public async Task<SendMessageResult> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        var session = await db.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.UserId == request.UserId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        var userMsg = ChatMessage.Create(session.Id, MessageRole.User, request.Content);
        db.ChatMessages.Add(userMsg);

        var priorMessages = session.Messages.ToList();
        var agentResult = await agentService.RunAsync(
            priorMessages,
            request.Content,
            cancellationToken);

        var assistantMsg = ChatMessage.Create(session.Id, MessageRole.Assistant, agentResult.Text);
        db.ChatMessages.Add(assistantMsg);

        foreach (var s in agentResult.Suggestions)
            db.Suggestions.Add(Suggestion.Create(assistantMsg.Id, s.Tag, s.CurrentValue, s.SuggestedValue));

        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var suggestionDtos = agentResult.Suggestions
            .Select(s => new SuggestionDto(s.Tag, s.CurrentValue, s.SuggestedValue))
            .ToList();

        return new SendMessageResult(
            new ChatMessageDto(assistantMsg.Id, "Assistant", agentResult.Text, assistantMsg.CreatedAt, suggestionDtos));
    }
}
