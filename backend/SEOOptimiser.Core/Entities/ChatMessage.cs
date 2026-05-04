namespace SEOOptimiser.Core.Entities;

public enum MessageRole { User, Assistant }

public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public ChatSession Session { get; private set; } = default!;

    private readonly List<Suggestion> _suggestions = [];
    public IReadOnlyCollection<Suggestion> Suggestions => _suggestions.AsReadOnly();

    private ChatMessage() { }

    public static ChatMessage Create(Guid sessionId, MessageRole role, string content) =>
        new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
}
