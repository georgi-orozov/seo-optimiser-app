using MediatR;
using Microsoft.EntityFrameworkCore;
using SEOOptimiser.Core.Interfaces;

namespace SEOOptimiser.Core.UseCases.Sessions.Queries;

public record GetSessionByIdQuery(Guid SessionId, string UserId) : IRequest<SessionDetailDto?>;

/// <summary>Full session detail including the complete ordered message history.</summary>
/// <param name="Id">Unique session identifier.</param>
/// <param name="Title">Display title of the session.</param>
/// <param name="CreatedAt">UTC timestamp when the session was created.</param>
/// <param name="Messages">All messages in chronological order (oldest first).</param>
public record SessionDetailDto(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    IReadOnlyList<ChatMessageDto> Messages);

/// <summary>A single message within a chat session.</summary>
/// <param name="Id">Unique message identifier.</param>
/// <param name="Role">
/// Message author: <c>"User"</c> for user messages, <c>"Assistant"</c> for agent replies.
/// </param>
/// <param name="Content">
/// Raw message text. Assistant messages may embed suggestion JSON objects inline,
/// one per line: <c>{"tag":"...","currentValue":"...","suggestedValue":"..."}</c>
/// </param>
/// <param name="CreatedAt">UTC timestamp when the message was persisted.</param>
public record ChatMessageDto(Guid Id, string Role, string Content, DateTime CreatedAt);

public class GetSessionByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSessionByIdQuery, SessionDetailDto?>
{
    public async Task<SessionDetailDto?> Handle(
        GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await db.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.UserId == request.UserId,
                cancellationToken);

        if (session is null) return null;

        return new SessionDetailDto(
            session.Id,
            session.Title,
            session.CreatedAt,
            session.Messages
                .Select(m => new ChatMessageDto(m.Id, m.Role.ToString(), m.Content, m.CreatedAt))
                .ToList());
    }
}
