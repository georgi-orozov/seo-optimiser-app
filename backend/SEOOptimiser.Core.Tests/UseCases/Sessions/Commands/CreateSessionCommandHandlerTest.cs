using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SEOOptimiser.Core.UseCases.Sessions.Commands;
using SEOOptimiser.Infrastructure.Persistence;

namespace SEOOptimiser.Core.Tests.UseCases.Sessions.Commands;

public class CreateSessionCommandHandlerTest
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateSessionCommandHandler CreateHandler(AppDbContext ctx) =>
        new(ctx, new Mock<ILogger<CreateSessionCommandHandler>>().Object);

    [Fact]
    public async Task Handle_WithValidCommand_PersistsSessionToDatabase()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        await handler.Handle(new CreateSessionCommand("user-1", "Test Session"), CancellationToken.None);

        ctx.ChatSessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNonEmptyId()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(new CreateSessionCommand("user-1", "Test Session"), CancellationToken.None);

        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCorrectTitle()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(new CreateSessionCommand("user-1", "My Title"), CancellationToken.None);

        result.Title.Should().Be("My Title");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCreatedAtTimestamp()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        var before = DateTime.UtcNow;

        var result = await handler.Handle(new CreateSessionCommand("user-1", "Test"), CancellationToken.None);

        result.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistedSessionHasCorrectUserId()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(new CreateSessionCommand("user-42", "Session"), CancellationToken.None);

        var persisted = await ctx.ChatSessions.FindAsync(result.Id);
        persisted!.UserId.Should().Be("user-42");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnedIdMatchesPersistedSession()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var result = await handler.Handle(new CreateSessionCommand("user-1", "Session"), CancellationToken.None);

        var persisted = await ctx.ChatSessions.FindAsync(result.Id);
        persisted.Should().NotBeNull();
    }
}
