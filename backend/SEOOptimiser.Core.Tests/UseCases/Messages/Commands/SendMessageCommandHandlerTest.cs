using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.Interfaces;
using SEOOptimiser.Core.UseCases.Messages.Commands;
using SEOOptimiser.Infrastructure.Persistence;

namespace SEOOptimiser.Core.Tests.UseCases.Messages.Commands;

public class SendMessageCommandHandlerTest
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SendMessageCommandHandler CreateHandler(
        AppDbContext ctx, IAgentService agentService) =>
        new(ctx, agentService, new Mock<ILogger<SendMessageCommandHandler>>().Object);

    private static Mock<IAgentService> DefaultAgentMock(
        string replyText = "Here are your SEO suggestions.",
        IReadOnlyList<SuggestionCapture>? suggestions = null)
    {
        var mock = new Mock<IAgentService>();
        mock.Setup(a => a.RunAsync(
                It.IsAny<IReadOnlyList<ChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentRunResult(replyText, suggestions ?? []));
        return mock;
    }

    private static async Task<ChatSession> SeedSessionAsync(AppDbContext ctx, string userId = "user-1")
    {
        var session = ChatSession.Create(userId, "Test Session");
        ctx.ChatSessions.Add(session);
        await ctx.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsKeyNotFoundException()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx, DefaultAgentMock().Object);
        var command = new SendMessageCommand(Guid.NewGuid(), "user-1", "Hello");

        await FluentActions.Awaiting(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToDifferentUser_ThrowsKeyNotFoundException()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx, "user-2");
        var handler = CreateHandler(ctx, DefaultAgentMock().Object);
        var command = new SendMessageCommand(session.Id, "user-1", "Hello");

        await FluentActions.Awaiting(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsUserMessage()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var handler = CreateHandler(ctx, DefaultAgentMock().Object);

        await handler.Handle(new SendMessageCommand(session.Id, "user-1", "Hello"), CancellationToken.None);

        ctx.ChatMessages.Should().Contain(m => m.Role == MessageRole.User && m.Content == "Hello");
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsAssistantMessage()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var handler = CreateHandler(ctx, DefaultAgentMock("Agent reply").Object);

        await handler.Handle(new SendMessageCommand(session.Id, "user-1", "Hello"), CancellationToken.None);

        ctx.ChatMessages.Should().Contain(m => m.Role == MessageRole.Assistant && m.Content == "Agent reply");
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsAgentServiceWithUserContent()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var agentMock = DefaultAgentMock();
        var handler = CreateHandler(ctx, agentMock.Object);

        await handler.Handle(new SendMessageCommand(session.Id, "user-1", "Analyse this"), CancellationToken.None);

        agentMock.Verify(a => a.RunAsync(
            It.IsAny<IReadOnlyList<ChatMessage>>(),
            "Analyse this",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsSuggestionsFromAgentResult()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var suggestions = new List<SuggestionCapture>
        {
            new("title", "Old Title", "New Title"),
            new("meta description", null, "Better description")
        };
        var handler = CreateHandler(ctx, DefaultAgentMock("Reply", suggestions).Object);

        await handler.Handle(new SendMessageCommand(session.Id, "user-1", "Hello"), CancellationToken.None);

        ctx.Suggestions.Should().HaveCount(2);
        ctx.Suggestions.Should().Contain(s => s.Tag == "title" && s.SuggestedValue == "New Title");
        ctx.Suggestions.Should().Contain(s => s.Tag == "meta description" && s.SuggestedValue == "Better description");
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesSessionUpdatedAt()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var originalUpdatedAt = session.UpdatedAt;
        var handler = CreateHandler(ctx, DefaultAgentMock().Object);

        await handler.Handle(new SendMessageCommand(session.Id, "user-1", "Hello"), CancellationToken.None);

        var updatedSession = await ctx.ChatSessions.FindAsync(session.Id);
        updatedSession!.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSendMessageResultWithAssistantRole()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var handler = CreateHandler(ctx, DefaultAgentMock("Agent reply").Object);

        var result = await handler.Handle(
            new SendMessageCommand(session.Id, "user-1", "Hello"), CancellationToken.None);

        result.AssistantMessage.Role.Should().Be("Assistant");
        result.AssistantMessage.Content.Should().Be("Agent reply");
    }

    [Fact]
    public async Task Handle_WithNoSuggestionsFromAgent_ReturnsEmptySuggestionsList()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var handler = CreateHandler(ctx, DefaultAgentMock().Object);

        var result = await handler.Handle(
            new SendMessageCommand(session.Id, "user-1", "Hello"), CancellationToken.None);

        result.AssistantMessage.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSuggestionsFromAgent_ReturnsMappedSuggestionDtos()
    {
        await using var ctx = CreateContext();
        var session = await SeedSessionAsync(ctx);
        var suggestions = new List<SuggestionCapture>
        {
            new("title", "Old", "New Title"),
        };
        var handler = CreateHandler(ctx, DefaultAgentMock("Reply", suggestions).Object);

        var result = await handler.Handle(
            new SendMessageCommand(session.Id, "user-1", "Hello"), CancellationToken.None);

        result.AssistantMessage.Suggestions.Should().HaveCount(1);
        result.AssistantMessage.Suggestions![0].Tag.Should().Be("title");
        result.AssistantMessage.Suggestions[0].CurrentValue.Should().Be("Old");
        result.AssistantMessage.Suggestions[0].SuggestedValue.Should().Be("New Title");
    }
}
