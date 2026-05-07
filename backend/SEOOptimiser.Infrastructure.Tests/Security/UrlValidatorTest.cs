using FluentAssertions;
using SEOOptimiser.Infrastructure.Security;

namespace SEOOptimiser.Infrastructure.Tests.Security;

public class UrlValidatorTest
{
    // Valid URLs

    [Fact]
    public async Task ValidateAsync_WithValidHttpsUrl_ReturnsOk()
    {
        var result = await UrlValidator.ValidateAsync("https://example.com/page");

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithValidHttpUrl_ReturnsOk()
    {
        var result = await UrlValidator.ValidateAsync("http://example.com/page");

        result.IsValid.Should().BeTrue();
    }

    // Invalid schemes

    [Fact]
    public async Task ValidateAsync_WithFtpScheme_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("ftp://example.com/file.txt");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithFileScheme_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("file:///etc/passwd");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithJavascriptScheme_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("javascript:alert(1)");

        result.IsValid.Should().BeFalse();
    }

    // Malformed URLs

    [Fact]
    public async Task ValidateAsync_WithMalformedUrl_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("not-a-url");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyString_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync(string.Empty);

        result.IsValid.Should().BeFalse();
    }

    // Blocked hostnames

    [Fact]
    public async Task ValidateAsync_WithLocalhost_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://localhost/api");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithLocalhostHttps_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("https://localhost:5000/api");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_With169_254_169_254_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://169.254.169.254/latest/meta-data/");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithMetadataGoogleInternal_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://metadata.google.internal/computeMetadata/v1/");

        result.IsValid.Should().BeFalse();
    }

    // Literal private IPs

    [Fact]
    public async Task ValidateAsync_WithLiteralPrivateIp10_0_0_1_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://10.0.0.1/api");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithLiteralPrivateIp192_168_1_1_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://192.168.1.1/");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithLiteralPrivateIp172_16_0_1_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://172.16.0.1/");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithLoopback127_0_0_1_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://127.0.0.1/");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithIpv6Loopback_ReturnsFail()
    {
        var result = await UrlValidator.ValidateAsync("http://[::1]/");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithLiteralPrivateIp172_31_255_255_ReturnsFail()
    {
        // Upper boundary of 172.16.0.0/12 range
        var result = await UrlValidator.ValidateAsync("http://172.31.255.255/");

        result.IsValid.Should().BeFalse();
    }

    // Failure result structure

    [Fact]
    public async Task ValidateAsync_WhenFailing_ReturnsNonNullErrorMessage()
    {
        var result = await UrlValidator.ValidateAsync("http://localhost/");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
