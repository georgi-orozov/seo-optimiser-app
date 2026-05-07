using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.UseCases.Sessions.Queries;
using SEOOptimiser.Infrastructure.Persistence;

namespace SEOOptimiser.Core.Tests.UseCases.Sessions.Queries;

public class GetSessionByIdQueryHandlerTest
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static GetSessionByIdQueryHandler CreateHandler(AppDbContext ctx) =>
        new(ctx, new Mock<ILogger<GetSessionByIdQueryHandler>>().Object);

    [Fact]
    public async Task Handle_WhenSessionNotFound_ReturnsNull()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(Guid.NewGuid(), "user-1"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToDifferentUser_ReturnsNull()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-2", "Private Session");
        ctx.ChatSessions.Add(session);
        await ctx.SaveChangesAsync();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(session.Id, "user-1"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsSessionDetailDto()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-1", "My Session");
        ctx.ChatSessions.Add(session);
        await ctx.SaveChangesAsync();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(session.Id, "user-1"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
        result.Title.Should().Be("My Session");
        result.CreatedAt.Should().Be(session.CreatedAt);
    }

    [Fact]
    public async Task Handle_WithMessages_ReturnsAllMessages()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-1", "Session");
        ctx.ChatSessions.Add(session);
        ctx.ChatMessages.Add(ChatMessage.Create(session.Id, MessageRole.User, "Hi"));
        ctx.ChatMessages.Add(ChatMessage.Create(session.Id, MessageRole.Assistant, "Hello!"));
        await ctx.SaveChangesAsync();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(session.Id, "user-1"), CancellationToken.None);

        result!.Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithMessages_MapsRoleAsString()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-1", "Session");
        ctx.ChatSessions.Add(session);
        ctx.ChatMessages.Add(ChatMessage.Create(session.Id, MessageRole.User, "Hi"));
        ctx.ChatMessages.Add(ChatMessage.Create(session.Id, MessageRole.Assistant, "Hello!"));
        await ctx.SaveChangesAsync();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(session.Id, "user-1"), CancellationToken.None);

        result!.Messages.Should().Contain(m => m.Role == "User");
        result.Messages.Should().Contain(m => m.Role == "Assistant");
    }

    [Fact]
    public async Task Handle_WithMessageWithSuggestions_MapsSuggestionsCorrectly()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-1", "Session");
        ctx.ChatSessions.Add(session);
        var msg = ChatMessage.Create(session.Id, MessageRole.Assistant, "Here are suggestions");
        ctx.ChatMessages.Add(msg);
        ctx.Suggestions.Add(Suggestion.Create(msg.Id, "title", "Old Title", "New Title"));
        await ctx.SaveChangesAsync();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(session.Id, "user-1"), CancellationToken.None);

        var assistantMsg = result!.Messages.Single(m => m.Role == "Assistant");
        assistantMsg.Suggestions.Should().HaveCount(1);
        assistantMsg.Suggestions![0].Tag.Should().Be("title");
        assistantMsg.Suggestions[0].CurrentValue.Should().Be("Old Title");
        assistantMsg.Suggestions[0].SuggestedValue.Should().Be("New Title");
    }

    [Fact]
    public async Task Handle_WithMessageWithNoSuggestions_ReturnsSuggestionsNull()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-1", "Session");
        ctx.ChatSessions.Add(session);
        ctx.ChatMessages.Add(ChatMessage.Create(session.Id, MessageRole.User, "Hello"));
        await ctx.SaveChangesAsync();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(session.Id, "user-1"), CancellationToken.None);

        result!.Messages[0].Suggestions.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNoMessages_ReturnsEmptyMessageList()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-1", "Empty Session");
        ctx.ChatSessions.Add(session);
        await ctx.SaveChangesAsync();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(
            new GetSessionByIdQuery(session.Id, "user-1"), CancellationToken.None);

        result!.Messages.Should().BeEmpty();
    }
}
