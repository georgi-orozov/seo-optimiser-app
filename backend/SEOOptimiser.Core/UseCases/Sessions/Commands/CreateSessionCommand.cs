using MediatR;
using Microsoft.EntityFrameworkCore;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.Interfaces;

namespace SEOOptimiser.Core.UseCases.Sessions.Commands;

public record CreateSessionCommand(string UserId, string Title) : IRequest<CreateSessionResult>;

/// <summary>Result returned after a session is created.</summary>
/// <param name="Id">Unique session identifier (GUID). Use this in subsequent message requests.</param>
/// <param name="Title">Display title of the session.</param>
/// <param name="CreatedAt">UTC timestamp when the session was created.</param>
public record CreateSessionResult(Guid Id, string Title, DateTime CreatedAt);

public class CreateSessionCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateSessionCommand, CreateSessionResult>
{
    public async Task<CreateSessionResult> Handle(
        CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = ChatSession.Create(request.UserId, request.Title);
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateSessionResult(session.Id, session.Title, session.CreatedAt);
    }
}
