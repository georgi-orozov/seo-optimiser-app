using FluentAssertions;
using SEOOptimiser.API.Controllers;

namespace SEOOptimiser.API.Tests.Models;

public class SendMessageRequestTest
{
    [Fact]
    public void SanitizedContent_WithNormalText_ReturnsUnchanged()
    {
        var req = new SendMessageRequest("Hello, world! 123");

        req.SanitizedContent.Should().Be("Hello, world! 123");
    }

    [Fact]
    public void SanitizedContent_WithNullBytes_RemovesNullBytes()
    {
        var req = new SendMessageRequest("Hello\x00World");

        req.SanitizedContent.Should().Be("HelloWorld");
    }

    [Fact]
    public void SanitizedContent_WithC0ControlChars_RemovesControlChars()
    {
        // \x01 (SOH) and \x1F (US) are C0 control chars that should be stripped
        var req = new SendMessageRequest("Hello\x01World\x1F");

        req.SanitizedContent.Should().Be("HelloWorld");
    }

    [Fact]
    public void SanitizedContent_PreservesTab()
    {
        var req = new SendMessageRequest("Hello\tWorld");

        req.SanitizedContent.Should().Be("Hello\tWorld");
    }

    [Fact]
    public void SanitizedContent_PreservesLineFeed()
    {
        var req = new SendMessageRequest("Hello\nWorld");

        req.SanitizedContent.Should().Be("Hello\nWorld");
    }

    [Fact]
    public void SanitizedContent_PreservesCarriageReturn()
    {
        var req = new SendMessageRequest("Hello\rWorld");

        req.SanitizedContent.Should().Be("Hello\rWorld");
    }

    [Fact]
    public void SanitizedContent_WithMultipleControlChars_RemovesAll()
    {
        // Mix of stripped chars (\x02, \x0B) and preserved chars (\t=\x09, \n=\x0A)
        var req = new SendMessageRequest("\x02Hello\x0BWorld\t\n");

        req.SanitizedContent.Should().Be("HelloWorld\t\n");
    }

    [Fact]
    public void SanitizedContent_WithDeleteChar_RemovesIt()
    {
        // \x7F (DEL) is in the strip range
        var req = new SendMessageRequest("Hello\x7FWorld");

        req.SanitizedContent.Should().Be("HelloWorld");
    }

    [Fact]
    public void SanitizedContent_WithUnicodeContent_PreservesUnicode()
    {
        var req = new SendMessageRequest("Héllo Wörld 🌍");

        req.SanitizedContent.Should().Be("Héllo Wörld 🌍");
    }
}
