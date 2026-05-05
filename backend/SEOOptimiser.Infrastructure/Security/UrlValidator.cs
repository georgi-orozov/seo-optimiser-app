using System.Net;
using System.Net.Sockets;

namespace SEOOptimiser.Infrastructure.Security;

internal static class UrlValidator
{
    private static readonly HashSet<string> BlockedHostnames = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "169.254.169.254",
        "metadata.google.internal",
        "metadata.goog",
        "instance-data",
    };

    public static async Task<UrlValidationResult> ValidateAsync(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return UrlValidationResult.Fail("Invalid URL format.");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return UrlValidationResult.Fail("Only http:// and https:// URLs are allowed.");

        var host = uri.Host;

        if (BlockedHostnames.Contains(host))
            return UrlValidationResult.Fail("URL host is not allowed.");

        // Literal IP in the URL — check before DNS to block e.g. http://10.0.0.1
        if (IPAddress.TryParse(host, out var literalIp) && IsPrivateOrLoopback(literalIp))
            return UrlValidationResult.Fail("URL resolves to a private or loopback address.");

        // DNS resolution — guards against DNS rebinding (public hostname → private IP)
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host);
        }
        catch (SocketException)
        {
            return UrlValidationResult.Fail("Could not resolve hostname.");
        }

        if (addresses.Length == 0)
            return UrlValidationResult.Fail("Hostname resolved to no addresses.");

        foreach (var ip in addresses)
        {
            if (IsPrivateOrLoopback(ip))
                return UrlValidationResult.Fail("URL resolves to a private or loopback address.");
        }

        return UrlValidationResult.Ok();
    }

    private static bool IsPrivateOrLoopback(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        // Unwrap IPv4-mapped IPv6 addresses (::ffff:10.x.x.x)
        var addr = ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv4MappedToIPv6
            ? ip.MapToIPv4()
            : ip;

        if (addr.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = addr.GetAddressBytes();
            // fc00::/7 — Unique Local Addresses
            return (bytes[0] & 0xFE) == 0xFC;
        }

        var o = addr.GetAddressBytes();
        return o[0] == 10                                       // 10.0.0.0/8
            || (o[0] == 172 && (o[1] & 0xF0) == 16)           // 172.16.0.0/12
            || (o[0] == 192 && o[1] == 168)                    // 192.168.0.0/16
            || (o[0] == 169 && o[1] == 254);                   // 169.254.0.0/16 (link-local / IMDS)
    }
}

internal record UrlValidationResult(bool IsValid, string? ErrorMessage)
{
    public static UrlValidationResult Ok()                       => new(true, null);
    public static UrlValidationResult Fail(string errorMessage)  => new(false, errorMessage);
}
