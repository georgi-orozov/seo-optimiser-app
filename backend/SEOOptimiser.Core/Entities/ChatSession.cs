namespace SEOOptimiser.Core.Entities;

public class ChatSession
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; set; }

    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private ChatSession() { }

    public static ChatSession Create(string userId, string title)
    {
        var now = DateTime.UtcNow;
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
