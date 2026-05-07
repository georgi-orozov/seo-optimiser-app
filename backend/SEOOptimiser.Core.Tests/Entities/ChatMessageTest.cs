using FluentAssertions;
using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Core.Tests.Entities;

public class ChatMessageTest
{
    [Fact]
    public void Create_WithValidArguments_SetsNonEmptyId()
    {
        var message = ChatMessage.Create(Guid.NewGuid(), MessageRole.User, "Hello");

        message.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithValidArguments_SetsSessionId()
    {
        var sessionId = Guid.NewGuid();
        var message = ChatMessage.Create(sessionId, MessageRole.User, "Hello");

        message.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void Create_WithUserRole_SetsRole()
    {
        var message = ChatMessage.Create(Guid.NewGuid(), MessageRole.User, "Hello");

        message.Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void Create_WithAssistantRole_SetsRole()
    {
        var message = ChatMessage.Create(Guid.NewGuid(), MessageRole.Assistant, "Response");

        message.Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void Create_WithValidArguments_SetsContent()
    {
        var message = ChatMessage.Create(Guid.NewGuid(), MessageRole.User, "My content");

        message.Content.Should().Be("My content");
    }

    [Fact]
    public void Create_WithValidArguments_SetsCreatedAtAsUtc()
    {
        var before = DateTime.UtcNow;
        var message = ChatMessage.Create(Guid.NewGuid(), MessageRole.User, "Hello");
        var after = DateTime.UtcNow;

        message.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        message.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_WithValidArguments_InitializesEmptySuggestions()
    {
        var message = ChatMessage.Create(Guid.NewGuid(), MessageRole.User, "Hello");

        message.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public void Create_MultipleCallsWithSameArguments_ProduceDifferentIds()
    {
        var sessionId = Guid.NewGuid();
        var msg1 = ChatMessage.Create(sessionId, MessageRole.User, "Hello");
        var msg2 = ChatMessage.Create(sessionId, MessageRole.User, "Hello");

        msg1.Id.Should().NotBe(msg2.Id);
    }
}
