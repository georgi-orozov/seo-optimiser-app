using System.Security.Claims;
using FluentAssertions;
using SEOOptimiser.API.Extensions;

namespace SEOOptimiser.API.Tests.Extensions;

public class ClaimsPrincipalExtensionsTest
{
    private static ClaimsPrincipal BuildPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));

    [Fact]
    public void GetUserId_WithNameIdentifierClaim_ReturnsUserId()
    {
        var principal = BuildPrincipal(new Claim(ClaimTypes.NameIdentifier, "user-abc"));

        var userId = principal.GetUserId();

        userId.Should().Be("user-abc");
    }

    [Fact]
    public void GetUserId_WithSubClaim_ReturnsUserId()
    {
        var principal = BuildPrincipal(new Claim("sub", "user-xyz"));

        var userId = principal.GetUserId();

        userId.Should().Be("user-xyz");
    }

    [Fact]
    public void GetUserId_WithNameIdentifierAndSubClaims_PrefersNameIdentifier()
    {
        var principal = BuildPrincipal(
            new Claim(ClaimTypes.NameIdentifier, "from-name-id"),
            new Claim("sub", "from-sub"));

        var userId = principal.GetUserId();

        userId.Should().Be("from-name-id");
    }

    [Fact]
    public void GetUserId_WithNoRelevantClaims_ThrowsInvalidOperationException()
    {
        var principal = BuildPrincipal(new Claim("email", "test@example.com"));

        var act = () => principal.GetUserId();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*User ID claim not found*");
    }

    [Fact]
    public void GetUserId_WithNoClaims_ThrowsInvalidOperationException()
    {
        var principal = BuildPrincipal();

        var act = () => principal.GetUserId();

        act.Should().Throw<InvalidOperationException>();
    }
}
