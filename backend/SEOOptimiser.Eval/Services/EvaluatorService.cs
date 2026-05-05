using HtmlAgilityPack;
using SEOOptimiser.Eval.Infrastructure;
using SEOOptimiser.Eval.Models;

namespace SEOOptimiser.Eval.Services;

public sealed class EvaluatorService
{
    private readonly AnthropicHaikuClient _haiku;

    private const string SystemPrompt = """
        You are a senior SEO expert and QA evaluator. Score an AI agent's SEO suggestions
        on a scale of 0-100 using this rubric:

        SCORING DIMENSIONS:
        1. Completeness [20pts]
           Did the agent produce exactly 3 suggestions for each of: title, meta description, h1?
           Full marks = 9 suggestions total with correct tag names.
           Deduct 2pts per missing suggestion, 1pt per wrong tag name.

        2. Keyword Integration [25pts]
           Do the suggested values naturally include the target keywords?
           Full marks = all suggestions include at least one target keyword appropriately.
           Deduct proportionally for suggestions that ignore the keywords entirely.

        3. SEO Compliance [25pts]
           Do suggestions follow technical SEO rules?
           - Titles: 50-60 characters
           - Meta descriptions: 150-160 characters
           - H1: contains primary keyword, reads naturally
           Deduct 3pts per suggestion that violates a rule (max 9pts deducted per dimension).

        4. Quality and Naturalness [20pts]
           Are the suggestions well-written, non-spammy, human-readable, and compelling?
           Full marks = all suggestions read naturally with a clear value proposition.
           Deduct proportionally for keyword stuffing, robotic phrasing, or generic filler.

        5. Differentiation [10pts]
           Are the 3 suggestions per tag meaningfully distinct from each other?
           Full marks = each of the 3 options offers a genuinely different angle or approach.
           Deduct proportionally for near-duplicate suggestions.

        Return ONLY valid JSON matching exactly this schema (no markdown, no explanation):
        {
          "score": <integer 0-100>,
          "reasoning": "<2-4 sentences explaining the overall score>",
          "strengths": ["<strength 1>", "<strength 2>"],
          "weaknesses": ["<weakness 1>", "<weakness 2>"]
        }

        strengths and weaknesses must each have between 1 and 3 items.
        """;

    public EvaluatorService(AnthropicHaikuClient haiku)
    {
        _haiku = haiku;
    }

    public async Task<EvalScore> ScoreAsync(
        TestCase testCase,
        AgentOutput agentOutput,
        CancellationToken ct = default)
    {
        var original = ExtractSeoTags(testCase.FakePageHtml);

        var titleSuggestions = FormatSuggestions(agentOutput.Suggestions.Where(s =>
            s.Tag.Equals("title", StringComparison.OrdinalIgnoreCase)));
        var metaSuggestions = FormatSuggestions(agentOutput.Suggestions.Where(s =>
            s.Tag.Equals("meta description", StringComparison.OrdinalIgnoreCase)));
        var h1Suggestions = FormatSuggestions(agentOutput.Suggestions.Where(s =>
            s.Tag.Equals("h1", StringComparison.OrdinalIgnoreCase)));

        var userMessage = $"""
            ## Test Case Context
            Target Keywords: {string.Join(", ", testCase.TargetKeywords)}
            Scenario: {testCase.Description}

            ## Original Page SEO Tags
            Title:            {original.Title ?? "(none)"}
            Meta Description: {original.MetaDescription ?? "(none)"}
            H1:               {original.H1 ?? "(none)"}
            OG Title:         {original.OgTitle ?? "(none)"}
            OG Description:   {original.OgDescription ?? "(none)"}
            Canonical:        {original.Canonical ?? "(none)"}

            ## Agent Suggestions ({agentOutput.Suggestions.Count} total)

            ### Title (3 required)
            {(titleSuggestions.Length > 0 ? titleSuggestions : "(none provided)")}

            ### Meta Description (3 required)
            {(metaSuggestions.Length > 0 ? metaSuggestions : "(none provided)")}

            ### H1 (3 required)
            {(h1Suggestions.Length > 0 ? h1Suggestions : "(none provided)")}

            ## Agent Summary Text
            {agentOutput.Text}

            Score these suggestions using your rubric. Return JSON only.
            """;

        return await _haiku.CompleteJsonAsync<EvalScore>(SystemPrompt, userMessage, ct);
    }

    private static string FormatSuggestions(IEnumerable<SuggestionRecord> suggestions)
    {
        var lines = suggestions.Select((s, i) =>
            $"  Option {i + 1}: \"{s.SuggestedValue}\" ({s.SuggestedValue.Length} chars)" +
            (s.CurrentValue is not null ? $" [was: \"{s.CurrentValue}\"]" : ""));
        return string.Join("\n", lines);
    }

    private static (string? Title, string? MetaDescription, string? H1,
                    string? OgTitle, string? OgDescription, string? Canonical)
        ExtractSeoTags(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        string? Attr(string xpath, string attr) =>
            doc.DocumentNode.SelectSingleNode(xpath)?.GetAttributeValue(attr, null);

        return (
            Title:          doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim(),
            MetaDescription: Attr("//meta[@name='description']", "content"),
            H1:             doc.DocumentNode.SelectSingleNode("//h1")?.InnerText.Trim(),
            OgTitle:        Attr("//meta[@property='og:title']", "content"),
            OgDescription:  Attr("//meta[@property='og:description']", "content"),
            Canonical:      Attr("//link[@rel='canonical']", "href")
        );
    }
}
