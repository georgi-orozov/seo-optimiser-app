namespace SEOOptimiser.Core.Entities;

public class Suggestion
{
    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public string Tag { get; private set; } = default!;
    public string? CurrentValue { get; private set; }
    public string SuggestedValue { get; private set; } = default!;

    public ChatMessage Message { get; private set; } = default!;

    private Suggestion() { }

    public static Suggestion Create(Guid messageId, string tag, string? currentValue, string suggestedValue) =>
        new()
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            Tag = tag,
            CurrentValue = currentValue,
            SuggestedValue = suggestedValue
        };
}
