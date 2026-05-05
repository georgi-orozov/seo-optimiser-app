using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SEOOptimiser.Eval.Infrastructure;

public sealed class AnthropicHaikuClient : IDisposable
{
    private const string ModelId = "claude-haiku-4-5-20251001";
    private const string BaseUrl = "https://api.anthropic.com/v1/messages";

    private readonly HttpClient _http;

    public AnthropicHaikuClient(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _http.Timeout = TimeSpan.FromSeconds(120);
    }

    // Sends a single-turn request and returns the assistant's text. Retries once if
    // the caller's JSON parsing fails (indicated by passing a non-null retryHint).
    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        var text = await SendAsync(systemPrompt, userMessage, ct);

        // Attempt to strip markdown code fences that models sometimes add
        text = StripCodeFences(text);

        return text;
    }

    // Convenience wrapper: parse JSON from the response, retrying once with an
    // explicit JSON reminder if the initial parse fails.
    public async Task<T> CompleteJsonAsync<T>(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        var text = await CompleteAsync(systemPrompt, userMessage, ct);

        try
        {
            return JsonSerializer.Deserialize<T>(text, JsonOptions)
                ?? throw new InvalidOperationException("Haiku returned null JSON.");
        }
        catch (JsonException)
        {
            // Retry once with a corrective instruction appended
            var retryMessage = userMessage +
                "\n\nIMPORTANT: Your previous response could not be parsed as JSON. " +
                "Return ONLY the raw JSON object/array with no markdown, no code fences, " +
                "and no surrounding text.";

            var retryText = await CompleteAsync(systemPrompt, retryMessage, ct);
            return JsonSerializer.Deserialize<T>(retryText, JsonOptions)
                ?? throw new InvalidOperationException("Haiku returned null JSON after retry.");
        }
    }

    private async Task<string> SendAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        var body = new
        {
            model = ModelId,
            max_tokens = 4096,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } }
        };

        using var response = await _http.PostAsJsonAsync(BaseUrl, body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonObject>(ct)
            ?? throw new InvalidOperationException("Empty response from Anthropic API.");

        return json["content"]?[0]?["text"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Unexpected Anthropic API response shape.");
    }

    private static string StripCodeFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
                trimmed = trimmed[(firstNewline + 1)..];
            if (trimmed.EndsWith("```"))
                trimmed = trimmed[..^3].TrimEnd();
        }
        return trimmed;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public void Dispose() => _http.Dispose();
}
