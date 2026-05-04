using MediatR;
using Microsoft.EntityFrameworkCore;
using SEOOptimiser.Core.Interfaces;

namespace SEOOptimiser.Core.UseCases.Sessions.Queries;

public record GetSessionsQuery(string UserId) : IRequest<IReadOnlyList<SessionSummaryDto>>;

/// <summary>Lightweight session summary — no messages included.</summary>
/// <param name="Id">Unique session identifier.</param>
/// <param name="Title">Display title of the session.</param>
/// <param name="CreatedAt">UTC timestamp when the session was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last message exchange. Used for sort order.</param>
public record SessionSummaryDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);

public class GetSessionsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSessionsQuery, IReadOnlyList<SessionSummaryDto>>
{
    public async Task<IReadOnlyList<SessionSummaryDto>> Handle(
        GetSessionsQuery request, CancellationToken cancellationToken)
    {
        return await db.ChatSessions
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new SessionSummaryDto(s.Id, s.Title, s.CreatedAt, s.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
