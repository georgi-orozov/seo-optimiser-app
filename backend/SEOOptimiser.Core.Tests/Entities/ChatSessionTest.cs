using FluentAssertions;
using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Core.Tests.Entities;

public class ChatSessionTest
{
    [Fact]
    public void Create_WithValidArguments_SetsNonEmptyId()
    {
        var session = ChatSession.Create("user-1", "My Session");

        session.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithValidArguments_SetsUserId()
    {
        var session = ChatSession.Create("user-1", "My Session");

        session.UserId.Should().Be("user-1");
    }

    [Fact]
    public void Create_WithValidArguments_SetsTitle()
    {
        var session = ChatSession.Create("user-1", "My Session");

        session.Title.Should().Be("My Session");
    }

    [Fact]
    public void Create_WithValidArguments_SetsCreatedAtAsUtc()
    {
        var before = DateTime.UtcNow;
        var session = ChatSession.Create("user-1", "My Session");
        var after = DateTime.UtcNow;

        session.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        session.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_WithValidArguments_SetsUpdatedAtSameAsCreatedAt()
    {
        var session = ChatSession.Create("user-1", "My Session");

        session.UpdatedAt.Should().Be(session.CreatedAt);
    }

    [Fact]
    public void Create_WithValidArguments_InitializesEmptyMessages()
    {
        var session = ChatSession.Create("user-1", "My Session");

        session.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Create_MultipleCallsWithSameArguments_ProduceDifferentIds()
    {
        var session1 = ChatSession.Create("user-1", "Session");
        var session2 = ChatSession.Create("user-1", "Session");

        session1.Id.Should().NotBe(session2.Id);
    }
}
