using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using Anthropic;
using HtmlAgilityPack;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SEOOptimiser.Core.Constants;
using SEOOptimiser.Core.Entities;
using SEOOptimiser.Core.Interfaces;
using CoreChatMessage = SEOOptimiser.Core.Entities.ChatMessage;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace SEOOptimiser.Infrastructure.Services;

public sealed class SeoAgentService : IAgentService
{
    // Intentionally weak SEO so the agent always has meaningful suggestions to make.
    private const string FakePageHtml = """
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

    private readonly ChatClientAgent _agent;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly bool _useFakePage;
    private readonly ILogger<SeoAgentService> _logger;

    public SeoAgentService(
        IHttpClientFactory httpClientFactory,
        ILogger<SeoAgentService> logger,
        string anthropicApiKey,
        bool useFakePage = false)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _useFakePage = useFakePage;

        var fetchTool = AIFunctionFactory.Create(
            ([Description("The URL of the web page to fetch and analyse.")] string url) =>
                FetchPageAsync(url),
            name: "fetch_page",
            description: "Fetches a web page at the given URL and extracts key SEO tags: title, meta description, h1, og:title, og:description, and canonical URL.");

        var anthropicClient = new AnthropicClient { ApiKey = anthropicApiKey };

        _agent = anthropicClient.AsAIAgent(
            model: "claude-sonnet-4-5",
            instructions: AgentSystemPrompt.Text,
            name: "SeoAgent",
            tools: [fetchTool]);
    }

    public async Task<string> RunAsync(
        IReadOnlyList<CoreChatMessage> priorMessages,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var agentSession = await _agent.CreateSessionAsync(cancellationToken);

        if (priorMessages.Count > 0)
        {
            var history = priorMessages
                .Select(m => new AIChatMessage(
                    m.Role == MessageRole.User ? ChatRole.User : ChatRole.Assistant,
                    m.Content))
                .ToList();

            agentSession.SetInMemoryChatHistory(history);
        }

        var response = await _agent.RunAsync(userMessage, agentSession, options: null, cancellationToken);
        return response.Text;
    }

    private async Task<string> FetchPageAsync(string url)
    {
        string html;

        if (_useFakePage)
        {
            _logger.LogWarning(
                "PageFetcher:UseFake is enabled — returning fake HTML instead of fetching '{Url}'.",
                url);
            html = FakePageHtml;
        }
        else
        {
            using var client = _httpClientFactory.CreateClient("PageFetcher");
            try
            {
                html = await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = $"Failed to fetch page: {ex.Message}" });
            }
        }

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
}
