using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SEOOptimiser.API.Controllers;
using SEOOptimiser.Core.UseCases.Messages.Commands;
using SEOOptimiser.Core.UseCases.Sessions.Commands;
using SEOOptimiser.Core.UseCases.Sessions.Queries;

namespace SEOOptimiser.API.Tests.Controllers;

public class SessionsControllerTest
{
    private const string UserId = "user-123";

    private static SessionsController CreateController(IMediator mediator)
    {
        var controller = new SessionsController(
            mediator,
            new Mock<ILogger<SessionsController>>().Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, UserId)],
                    "Test"))
            }
        };

        return controller;
    }

    // CreateSession

    [Fact]
    public async Task CreateSession_WithValidRequest_Returns201Created()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSessionResult(Guid.NewGuid(), "Test Session", DateTime.UtcNow));
        var controller = CreateController(mediator.Object);

        var result = await controller.CreateSession(new CreateSessionRequest(null), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateSession_WithValidRequest_SendsCreateSessionCommandWithCorrectUserId()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSessionResult(Guid.NewGuid(), "New Session", DateTime.UtcNow));
        var controller = CreateController(mediator.Object);

        await controller.CreateSession(new CreateSessionRequest(null), CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<CreateSessionCommand>(c => c.UserId == UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WithNullTitle_SendsDefaultTitle()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSessionResult(Guid.NewGuid(), "New Session", DateTime.UtcNow));
        var controller = CreateController(mediator.Object);

        await controller.CreateSession(new CreateSessionRequest(null), CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<CreateSessionCommand>(c => c.Title == "New Session"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WithCustomTitle_SendsCustomTitle()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSessionResult(Guid.NewGuid(), "My Page", DateTime.UtcNow));
        var controller = CreateController(mediator.Object);

        await controller.CreateSession(new CreateSessionRequest("My Page"), CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<CreateSessionCommand>(c => c.Title == "My Page"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // GetSessions

    [Fact]
    public async Task GetSessions_WithValidUser_Returns200Ok()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetSessionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SessionSummaryDto>());
        var controller = CreateController(mediator.Object);

        var result = await controller.GetSessions(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetSessions_WithValidUser_SendsGetSessionsQueryWithCorrectUserId()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetSessionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SessionSummaryDto>());
        var controller = CreateController(mediator.Object);

        await controller.GetSessions(CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<GetSessionsQuery>(q => q.UserId == UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // GetSession

    [Fact]
    public async Task GetSession_WhenSessionExists_Returns200Ok()
    {
        var sessionId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetSessionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDetailDto(sessionId, "Session", DateTime.UtcNow, []));
        var controller = CreateController(mediator.Object);

        var result = await controller.GetSession(sessionId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetSession_WhenSessionNotFound_Returns404NotFound()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetSessionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionDetailDto?)null);
        var controller = CreateController(mediator.Object);

        var result = await controller.GetSession(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetSession_WithValidRequest_SendsCorrectSessionIdAndUserId()
    {
        var sessionId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetSessionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDetailDto(sessionId, "Session", DateTime.UtcNow, []));
        var controller = CreateController(mediator.Object);

        await controller.GetSession(sessionId, CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<GetSessionByIdQuery>(q => q.SessionId == sessionId && q.UserId == UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // SendMessage

    [Fact]
    public async Task SendMessage_WithValidRequest_Returns200Ok()
    {
        var sessionId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResult(
                new ChatMessageDto(Guid.NewGuid(), "Assistant", "Reply", DateTime.UtcNow, null)));
        var controller = CreateController(mediator.Object);

        var result = await controller.SendMessage(
            sessionId, new SendMessageRequest("Hello"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendMessage_WithValidRequest_SendsSendMessageCommandWithCorrectIds()
    {
        var sessionId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResult(
                new ChatMessageDto(Guid.NewGuid(), "Assistant", "Reply", DateTime.UtcNow, null)));
        var controller = CreateController(mediator.Object);

        await controller.SendMessage(
            sessionId, new SendMessageRequest("Hello"), CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<SendMessageCommand>(c => c.SessionId == sessionId && c.UserId == UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_WithValidRequest_SendsSanitizedContent()
    {
        var sessionId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResult(
                new ChatMessageDto(Guid.NewGuid(), "Assistant", "Reply", DateTime.UtcNow, null)));
        var controller = CreateController(mediator.Object);

        await controller.SendMessage(
            sessionId, new SendMessageRequest("Hello\x00World"), CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<SendMessageCommand>(c => c.Content == "HelloWorld"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
