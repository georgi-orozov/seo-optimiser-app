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

/// <summary>A single structured SEO suggestion attached to an assistant message.</summary>
/// <param name="Tag">The HTML tag being suggested (e.g. "title", "meta description", "h1").</param>
/// <param name="CurrentValue">The page's existing value for this tag, or null if absent.</param>
/// <param name="SuggestedValue">The agent's recommended replacement value.</param>
public record SuggestionDto(string Tag, string? CurrentValue, string SuggestedValue);

/// <summary>A single message within a chat session.</summary>
/// <param name="Id">Unique message identifier.</param>
/// <param name="Role">
/// Message author: <c>"User"</c> for user messages, <c>"Assistant"</c> for agent replies.
/// </param>
/// <param name="Content">The message text.</param>
/// <param name="CreatedAt">UTC timestamp when the message was persisted.</param>
/// <param name="Suggestions">
/// Structured SEO suggestions captured during this turn via the <c>record_seo_suggestion</c> tool.
/// Non-null only on assistant messages that produced suggestions; null on user messages and
/// assistant messages with no suggestions.
/// </param>
public record ChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt,
    IReadOnlyList<SuggestionDto>? Suggestions);

public class GetSessionByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSessionByIdQuery, SessionDetailDto?>
{
    public async Task<SessionDetailDto?> Handle(
        GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await db.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Suggestions)
            .FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.UserId == request.UserId,
                cancellationToken);

        if (session is null) return null;

        return new SessionDetailDto(
            session.Id,
            session.Title,
            session.CreatedAt,
            session.Messages
                .Select(m => new ChatMessageDto(
                    m.Id,
                    m.Role.ToString(),
                    m.Content,
                    m.CreatedAt,
                    m.Suggestions.Count > 0
                        ? m.Suggestions
                            .Select(s => new SuggestionDto(s.Tag, s.CurrentValue, s.SuggestedValue))
                            .ToList()
                        : null))
                .ToList());
    }
}
