using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SEOOptimiser.API.Authentication;

namespace SEOOptimiser.API.Tests.Authentication;

public class DevBypassAuthHandlerTest
{
    private static async Task<AuthenticateResult> AuthenticateAsync()
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Setup(m => m.Get(DevBypassAuthHandler.SchemeName))
            .Returns(new AuthenticationSchemeOptions());
        options.Setup(m => m.CurrentValue)
            .Returns(new AuthenticationSchemeOptions());

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var handler = new DevBypassAuthHandler(
            options.Object,
            loggerFactory.Object,
            UrlEncoder.Default);

        var scheme = new AuthenticationScheme(
            DevBypassAuthHandler.SchemeName,
            DevBypassAuthHandler.SchemeName,
            typeof(DevBypassAuthHandler));

        await handler.InitializeAsync(scheme, new DefaultHttpContext());

        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Always_ReturnsSuccess()
    {
        var result = await AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Always_SetsNameIdentifierClaim()
    {
        var result = await AuthenticateAsync();

        var claim = result.Principal!.FindFirst(ClaimTypes.NameIdentifier);
        claim.Should().NotBeNull();
        claim!.Value.Should().Be("dev-user-bypass");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Always_InjectsDevUserBypassIdentity()
    {
        var result = await AuthenticateAsync();

        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
        result.Principal.Identity.AuthenticationType.Should().Be(DevBypassAuthHandler.SchemeName);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Always_UsesDevBypassSchemeName()
    {
        var result = await AuthenticateAsync();

        result.Ticket!.AuthenticationScheme.Should().Be(DevBypassAuthHandler.SchemeName);
    }
}
