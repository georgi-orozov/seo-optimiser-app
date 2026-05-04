using MediatR;
using Microsoft.EntityFrameworkCore;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.Interfaces;
using SEOOptimiser.Core.UseCases.Sessions.Queries;

namespace SEOOptimiser.Core.UseCases.Messages.Commands;

public record SendMessageCommand(Guid SessionId, string UserId, string Content)
    : IRequest<SendMessageResult>;

/// <summary>Result of sending a message — contains only the agent's reply.</summary>
/// <param name="AssistantMessage">
/// The assistant's reply. May contain inline suggestion JSON objects, one per line.
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
        var replyText = await agentService.RunAsync(
            session.Id.ToString(),
            priorMessages,
            request.Content,
            cancellationToken);

        var assistantMsg = ChatMessage.Create(session.Id, MessageRole.Assistant, replyText);
        db.ChatMessages.Add(assistantMsg);

        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new SendMessageResult(
            new ChatMessageDto(assistantMsg.Id, "Assistant", replyText, assistantMsg.CreatedAt));
    }
}
