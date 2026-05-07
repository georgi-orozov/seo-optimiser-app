using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.UseCases.Sessions.Queries;
using SEOOptimiser.Infrastructure.Persistence;

namespace SEOOptimiser.Core.Tests.UseCases.Sessions.Queries;

public class GetSessionsQueryHandlerTest
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_WithNoSessions_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        var handler = new GetSessionsQueryHandler(ctx);

        var result = await handler.Handle(new GetSessionsQuery("user-1"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSessionsForDifferentUsers_ReturnsOnlyOwnerSessions()
    {
        await using var ctx = CreateContext();
        ctx.ChatSessions.AddRange(
            ChatSession.Create("user-1", "My Session"),
            ChatSession.Create("user-2", "Other Session"));
        await ctx.SaveChangesAsync();
        var handler = new GetSessionsQueryHandler(ctx);

        var result = await handler.Handle(new GetSessionsQuery("user-1"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("My Session");
    }

    [Fact]
    public async Task Handle_WithMultipleSessions_OrdersByUpdatedAtDescending()
    {
        await using var ctx = CreateContext();
        var older = ChatSession.Create("user-1", "Older");
        older.UpdatedAt = DateTime.UtcNow.AddHours(-2);
        var newer = ChatSession.Create("user-1", "Newer");
        newer.UpdatedAt = DateTime.UtcNow.AddHours(-1);
        ctx.ChatSessions.AddRange(older, newer);
        await ctx.SaveChangesAsync();
        var handler = new GetSessionsQueryHandler(ctx);

        var result = await handler.Handle(new GetSessionsQuery("user-1"), CancellationToken.None);

        result[0].Title.Should().Be("Newer");
        result[1].Title.Should().Be("Older");
    }

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsMappedDtos()
    {
        await using var ctx = CreateContext();
        var session = ChatSession.Create("user-1", "Test Session");
        ctx.ChatSessions.Add(session);
        await ctx.SaveChangesAsync();
        var handler = new GetSessionsQueryHandler(ctx);

        var result = await handler.Handle(new GetSessionsQuery("user-1"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(session.Id);
        result[0].Title.Should().Be("Test Session");
        result[0].CreatedAt.Should().Be(session.CreatedAt);
        result[0].UpdatedAt.Should().Be(session.UpdatedAt);
    }

    [Fact]
    public async Task Handle_WithMultipleSessionsForSameUser_ReturnsAll()
    {
        await using var ctx = CreateContext();
        ctx.ChatSessions.AddRange(
            ChatSession.Create("user-1", "Session A"),
            ChatSession.Create("user-1", "Session B"),
            ChatSession.Create("user-1", "Session C"));
        await ctx.SaveChangesAsync();
        var handler = new GetSessionsQueryHandler(ctx);

        var result = await handler.Handle(new GetSessionsQuery("user-1"), CancellationToken.None);

        result.Should().HaveCount(3);
    }
}
