using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOOptimiser.API.Extensions;
using SEOOptimiser.Core.UseCases.Messages.Commands;
using SEOOptimiser.Core.UseCases.Sessions.Commands;
using SEOOptimiser.Core.UseCases.Sessions.Queries;

namespace SEOOptimiser.API.Controllers;

/// <summary>
/// Manages SEO optimisation chat sessions and messages.
/// </summary>
[ApiController]
[Route("api/sessions")]
[Authorize]
[Produces("application/json")]
public partial class SessionsController(IMediator mediator, ILogger<SessionsController> logger) : ControllerBase
{
    /// <summary>
    /// Creates a new chat session for the authenticated user.
    /// </summary>
    /// <remarks>
    /// A session is the top-level container for a conversation with the SEO agent.
    /// Create one session per page or optimisation task. The agent's conversation
    /// history is persisted so context is preserved across subsequent message requests.
    /// </remarks>
    /// <param name="req">Request body. <c>Title</c> is optional; defaults to "New Session".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created session metadata including its ID.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSessionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateSessionRequest req,
        CancellationToken ct)
    {
        LogCreateSession(logger, User.GetUserId());
        var result = await mediator.Send(
            new CreateSessionCommand(User.GetUserId(), req.Title ?? "New Session"), ct);
        return CreatedAtAction(nameof(GetSession), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lists all chat sessions belonging to the authenticated user.
    /// </summary>
    /// <remarks>
    /// Sessions are returned in descending order of last activity (<c>UpdatedAt</c>),
    /// so the most recently active session appears first.
    /// Messages are not included; use <c>GET /api/sessions/{id}</c> to load a full session.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An array of session summaries (no messages).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SessionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        LogGetSessions(logger, User.GetUserId());
        var result = await mediator.Send(new GetSessionsQuery(User.GetUserId()), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single session with its full message history.
    /// </summary>
    /// <remarks>
    /// Messages are ordered chronologically (oldest first).
    /// The <c>Role</c> field is either <c>"User"</c> or <c>"Assistant"</c>.
    /// Assistant messages may contain inline suggestion JSON objects, one per line,
    /// in the format: <c>{"tag":"&lt;title&gt;","currentValue":"...","suggestedValue":"..."}</c>
    /// </remarks>
    /// <param name="id">Session ID (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Session detail including all messages, or 404 if not found or not owned by the caller.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken ct)
    {
        LogGetSession(logger, id, User.GetUserId());
        var result = await mediator.Send(new GetSessionByIdQuery(id, User.GetUserId()), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Sends a user message and returns the agent's reply.
    /// </summary>
    /// <remarks>
    /// This is the main interaction endpoint. The full conversation history is loaded
    /// from the database and replayed to the agent on every call, so context is preserved.
    ///
    /// **Typical flow:**
    /// 1. Send a URL: `"Analyse https://example.com"`
    /// 2. Agent fetches the page and asks for target keywords.
    /// 3. Send keywords: `"target keywords: SEO tools, meta optimisation"`
    /// 4. Agent returns 3 suggestions per tag as inline JSON.
    /// 5. Ask for refinements: `"Make the title more action-oriented"`
    ///
    /// **Suggestion format** (one object per line in the assistant reply):
    /// ```json
    /// {"tag":"&lt;title&gt;","currentValue":"Current Title","suggestedValue":"Better Title"}
    /// {"tag":"&lt;meta description&gt;","currentValue":"...","suggestedValue":"..."}
    /// {"tag":"&lt;h1&gt;","currentValue":"...","suggestedValue":"..."}
    /// ```
    /// </remarks>
    /// <param name="id">Session ID (GUID).</param>
    /// <param name="req">Request body containing the user's message text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assistant's reply message including its ID and timestamp.</returns>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(typeof(SendMessageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest req,
        CancellationToken ct)
    {
        LogSendMessage(logger, id, User.GetUserId());
        var result = await mediator.Send(new SendMessageCommand(id, User.GetUserId(), req.Content), ct);
        return Ok(result);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "CreateSession requested by user {UserId}")]
    private static partial void LogCreateSession(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "GetSessions requested by user {UserId}")]
    private static partial void LogGetSessions(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "GetSession requested for {SessionId} by user {UserId}")]
    private static partial void LogGetSession(ILogger logger, Guid sessionId, string userId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "SendMessage requested for session {SessionId} by user {UserId}")]
    private static partial void LogSendMessage(ILogger logger, Guid sessionId, string userId);
}

/// <summary>Request body for creating a new session.</summary>
/// <param name="Title">Optional display title. Defaults to "New Session" if omitted.</param>
public record CreateSessionRequest(string? Title);

/// <summary>Request body for sending a message to the agent.</summary>
/// <param name="Content">The user's message text. Can be a URL, keywords, or any free-form instruction.</param>
public record SendMessageRequest(string Content);
