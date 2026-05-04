using MediatR;
using Microsoft.Extensions.Logging;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.Interfaces;

namespace SEOOptimiser.Core.UseCases.Sessions.Commands;

public record CreateSessionCommand(string UserId, string Title) : IRequest<CreateSessionResult>;

/// <summary>Result returned after a session is created.</summary>
/// <param name="Id">Unique session identifier (GUID). Use this in subsequent message requests.</param>
/// <param name="Title">Display title of the session.</param>
/// <param name="CreatedAt">UTC timestamp when the session was created.</param>
public record CreateSessionResult(Guid Id, string Title, DateTime CreatedAt);

public partial class CreateSessionCommandHandler(IAppDbContext db, ILogger<CreateSessionCommandHandler> logger)
    : IRequestHandler<CreateSessionCommand, CreateSessionResult>
{
    public async Task<CreateSessionResult> Handle(
        CreateSessionCommand request, CancellationToken cancellationToken)
    {
        LogCreatingSession(logger, request.UserId, request.Title);

        var session = ChatSession.Create(request.UserId, request.Title);
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        LogSessionCreated(logger, session.Id, request.UserId);

        return new CreateSessionResult(session.Id, session.Title, session.CreatedAt);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Creating session for user {UserId} with title '{Title}'")]
    private static partial void LogCreatingSession(ILogger logger, string userId, string title);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Session {SessionId} created for user {UserId}")]
    private static partial void LogSessionCreated(ILogger logger, Guid sessionId, string userId);
}
