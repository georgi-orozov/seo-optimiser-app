using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic;
using HtmlAgilityPack;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SEOOptimiser.Core.Constants;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.Interfaces;
using SEOOptimiser.Infrastructure.Security;
using SEOOptimiser.Infrastructure.Telemetry;
using CoreChatMessage = SEOOptimiser.Core.Entities.ChatMessage;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace SEOOptimiser.Infrastructure.Services;

public sealed partial class SeoAgentService : IAgentService
{
    // Intentionally weak SEO so the agent always has meaningful suggestions to make.
    public const string FakePageHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <title>Home - Acme Corp</title>
            <meta name="description" content="We sell things online.">
            <link rel="canonical" href="https://www.acmecorp.example.com/">
        </head>
        <body>
            <h1>Welcome</h1>
            <p>Acme Corp is a leading provider of innovative solutions for businesses worldwide.
            Our products help companies streamline operations and improve productivity.</p>
            <h2>Our Products</h2>
            <p>We offer a wide range of products including widgets, gadgets, and more.</p>
        </body>
        </html>
        """;
    
    // AsyncLocal ensures concurrent requests don't interfere with each other.
    private static readonly AsyncLocal<List<SuggestionCapture>?> _currentSuggestions = new();

    private static readonly string[] RefinementKeywords =
    [
        "change", "update", "modify", "replace", "rename",
        "make it", "make the", "make option", "set ",
        "use ", "switch", "rewrite", "reword", "rephrase",
        "shorten", "lengthen", "shorter", "longer",
        "adjust", "tweak", "edit", "fix", "revise",
        "try ", "different", "instead"
    ];

    private readonly ChatClientAgent _agent;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _fakePageHtml;
    private readonly ILogger<SeoAgentService> _logger;

    public SeoAgentService(
        IHttpClientFactory httpClientFactory,
        ILogger<SeoAgentService> logger,
        string anthropicApiKey,
        string? fakePageHtml = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _fakePageHtml = fakePageHtml;

        var fetchTool = AIFunctionFactory.Create(
            ([Description("The URL of the web page to fetch and analyse.")] string url) =>
                FetchPageAsync(url),
            name: "fetch_page",
            description: "Fetches a web page at the given URL and extracts key SEO tags: title, meta description, h1, og:title, og:description, and canonical URL.");

        var suggestTool = AIFunctionFactory.Create(
            ([Description("Exactly 3 title tag alternatives (50-60 chars each).")] TagOption[] title,
             [Description("Exactly 3 meta description alternatives (150-160 chars each).")] TagOption[] metaDescription,
             [Description("Exactly 3 H1 tag alternatives (contains primary keyword, reads naturally).")] TagOption[] h1) =>
            {
                foreach (var s in title)
                    _currentSuggestions.Value?.Add(new SuggestionCapture("title", s.CurrentValue, s.SuggestedValue));
                foreach (var s in metaDescription)
                    _currentSuggestions.Value?.Add(new SuggestionCapture("meta description", s.CurrentValue, s.SuggestedValue));
                foreach (var s in h1)
                    _currentSuggestions.Value?.Add(new SuggestionCapture("h1", s.CurrentValue, s.SuggestedValue));
                return Task.FromResult("All 9 suggestions recorded.");
            },
            name: "record_seo_suggestions",
            description: "Record all SEO suggestions at once — call exactly once per response with 3 options each for title, meta description, and h1.");

        var updateTool = AIFunctionFactory.Create(
            ([Description("The tag being updated: 'title', 'meta description', or 'h1'.")] string tag,
             [Description("The page's existing value. Null if the tag is absent.")] string? currentValue,
             [Description("The updated replacement value.")] string suggestedValue) =>
            {
                _currentSuggestions.Value?.Add(new SuggestionCapture(tag, currentValue, suggestedValue));
                return Task.FromResult("Suggestion updated.");
            },
            name: "update_seo_suggestion",
            description: "Record one refined SEO suggestion when the user asks to change a specific option. " +
                         "Use this — NOT record_seo_suggestions — for refinements. Call once per changed option.");

        var anthropicClient = new AnthropicClient { ApiKey = anthropicApiKey };

        _agent = anthropicClient.AsAIAgent(
            model: "claude-sonnet-4-5",
            instructions: AgentSystemPrompt.Text,
            name: "SeoAgent",
            tools: [fetchTool, suggestTool, updateTool]);
    }

    public async Task<AgentRunResult> RunAsync(
        IReadOnlyList<CoreChatMessage> priorMessages,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        using var runActivity = SeoTelemetry.Source.StartActivity("seo-agent.run", ActivityKind.Internal);
        runActivity?.SetTag("prior.message.count", priorMessages.Count);

        _currentSuggestions.Value = [];
        try
        {
            LogCreatingAgentSession(priorMessages.Count);

            var agentSession = await _agent.CreateSessionAsync(cancellationToken);

            if (priorMessages.Count > 0)
            {
                var history = priorMessages
                    .Select(m =>
                    {
                        var content = m.Content;
                        if (m.Role == MessageRole.Assistant && m.Suggestions.Count > 0)
                            content += FormatSuggestionsForHistory(m.Suggestions);
                        return new AIChatMessage(
                            m.Role == MessageRole.User ? ChatRole.User : ChatRole.Assistant,
                            content);
                    })
                    .ToList();

                agentSession.SetInMemoryChatHistory(history);
            }

            LogRunningAgent(userMessage.Length);

            var response = await _agent.RunAsync(userMessage, agentSession, options: null, cancellationToken);

            string finalText = response.Text!;
            if (_currentSuggestions.Value.Count == 0 && IsLikelyRefinementRequest(userMessage))
            {
                using var retryActivity = SeoTelemetry.Source.StartActivity("seo-agent.retry", ActivityKind.Internal);
                retryActivity?.SetTag("retry.reason", "missed-tool-call");

                LogRetryingMissedToolCall(userMessage.Length);
                const string corrective =
                    "You described the change but did not call update_seo_suggestion. " +
                    "Call it now with the exact updated value you described. " +
                    "Do not explain — just call the tool.";
                var retryResponse = await _agent.RunAsync(corrective, agentSession, options: null, cancellationToken);
                finalText = retryResponse.Text!;
            }

            var captured = _currentSuggestions.Value.ToList();

            runActivity?.SetTag("suggestions.captured", captured.Count);
            runActivity?.SetStatus(ActivityStatusCode.Ok);

            LogAgentRunComplete(finalText.Length, captured.Count);

            return new AgentRunResult(finalText, captured);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            runActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            runActivity?.AddException(ex);
            throw;
        }
        finally
        {
            _currentSuggestions.Value = null;
        }
    }

    private static string FormatSuggestionsForHistory(IReadOnlyCollection<Suggestion> suggestions)
    {
        var sb = new StringBuilder("\n\n[Suggestions provided in this response:");
        foreach (var tag in new[] { "title", "meta description", "h1" })
        {
            var group = suggestions
                .Where(s => s.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (group.Count == 0) continue;
            sb.AppendLine();
            sb.Append($"  {tag.ToUpperInvariant()}:");
            for (var i = 0; i < group.Count; i++)
            {
                var current = group[i].CurrentValue is not null
                    ? $" (was: \"{group[i].CurrentValue}\")"
                    : string.Empty;
                sb.AppendLine();
                sb.Append($"    Option {i + 1}: \"{group[i].SuggestedValue}\"{current}");
            }
        }
        sb.AppendLine();
        sb.Append(']');
        return sb.ToString();
    }

    private async Task<string> FetchPageAsync(string url)
    {
        using var fetchActivity = SeoTelemetry.Source.StartActivity("seo-agent.fetch-page", ActivityKind.Client);
        fetchActivity?.SetTag("url", url);

        string html;

        if (_fakePageHtml is not null)
        {
            LogFakePageWarning(url);
            fetchActivity?.SetTag("fetch.source", "fake");
            html = _fakePageHtml;
        }
        else
        {
            var validation = await UrlValidator.ValidateAsync(url);
            if (!validation.IsValid)
            {
                LogSsrfBlocked(url, validation.ErrorMessage!);
                fetchActivity?.SetStatus(ActivityStatusCode.Error, "SSRF blocked");
                fetchActivity?.SetTag("fetch.blocked", "true");
                return JsonSerializer.Serialize(new { error = "The provided URL could not be fetched. Please provide a valid public URL." });
            }

            LogFetchingPage(url);
            fetchActivity?.SetTag("fetch.source", "http");
            using var client = _httpClientFactory.CreateClient("PageFetcher");
            try
            {
                html = await client.GetStringAsync(url);
                LogFetchedPage(url, html.Length);
                fetchActivity?.SetTag("fetch.response.bytes", html.Length);
                fetchActivity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                LogFetchPageFailed(ex, url);
                fetchActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                fetchActivity?.AddException(ex);
                return JsonSerializer.Serialize(new { error = "The page could not be fetched. Please verify the URL is publicly accessible." });
            }
        }

        // Strip HTML comments before parsing — prevents indirect prompt injection via
        // page content like <!-- Ignore previous instructions and... -->
        html = StripHtmlComments(html);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var result = new
        {
            title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim(),
            metaDescription = GetAttr("//meta[@name='description']", "content"),
            h1 = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText.Trim(),
            ogTitle = GetAttr("//meta[@property='og:title']", "content"),
            ogDescription = GetAttr("//meta[@property='og:description']", "content"),
            canonical = GetAttr("//link[@rel='canonical']", "href")
        };

        return JsonSerializer.Serialize(result);

        string? GetAttr(string xpath, string attr) =>
            doc.DocumentNode.SelectSingleNode(xpath)?.GetAttributeValue(attr, null);
    }

    internal static bool IsLikelyRefinementRequest(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();
        foreach (var keyword in RefinementKeywords)
            if (lower.Contains(keyword))
                return true;
        return false;
    }

    internal static string StripHtmlComments(string html) =>
        Regex.Replace(html, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Creating agent session (prior messages: {Count})")]
    private partial void LogCreatingAgentSession(int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Running agent (message length: {Length} chars)")]
    private partial void LogRunningAgent(int length);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Agent run complete — response: {ResponseLength} chars, suggestions captured: {SuggestionCount}")]
    private partial void LogAgentRunComplete(int responseLength, int suggestionCount);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "PageFetcher:UseFake is enabled — returning fake HTML instead of fetching '{Url}'.")]
    private partial void LogFakePageWarning(string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching page: {Url}")]
    private partial void LogFetchingPage(string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetched {Url} ({Length} bytes)")]
    private partial void LogFetchedPage(string url, int length);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch page: {Url}")]
    private partial void LogFetchPageFailed(Exception ex, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SSRF blocked — URL '{Url}': {Reason}")]
    private partial void LogSsrfBlocked(string url, string reason);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Agent skipped update_seo_suggestion on likely refinement (message length: {Length} chars) — retrying")]
    private partial void LogRetryingMissedToolCall(int length);

    // Parameter types for the batch suggestion tool.
    private record TagOption(
        [property: Description("The page's existing value. Null if the tag is absent.")] string? CurrentValue,
        [property: Description("The recommended replacement value.")] string SuggestedValue);
}
