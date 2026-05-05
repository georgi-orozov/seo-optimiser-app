using System.Text.Json;
using SEOOptimiser.Eval.Infrastructure;
using SEOOptimiser.Eval.Models;

namespace SEOOptimiser.Eval.Services;

public sealed class TestCaseGeneratorService
{
    private readonly AnthropicHaikuClient _haiku;

    private const string SystemPrompt = """
        You are a QA engineer for an SEO AI agent. Your task is to generate diverse, realistic
        test cases for evaluating the agent's SEO suggestion quality.

        Each test case must include a realistic HTML page with intentionally imperfect SEO,
        target keywords plausible for that page's niche, and a user message that mimics how
        a real user would start the session.

        Rotate through these scenario archetypes across the batch:
        1. MISSING_TAGS       — page has no meta description, no og: tags
        2. KEYWORD_MISMATCH   — existing title/h1/description don't match the target keywords
        3. TOO_SHORT          — title or description are too brief (under 30 chars)
        4. TOO_LONG           — title or description are too long (over 70 chars / 200 chars)
        5. DUPLICATE          — title and h1 are identical verbatim
        6. NO_H1              — page body has no h1 element at all
        7. GOOD_BASELINE      — reasonably good SEO to verify the agent doesn't over-suggest
        8. ECOMMERCE          — product page with price signals and commercial keywords
        9. LOCAL_BUSINESS     — local SEO page with geo-specific keywords
        10. TECHNICAL_BLOG    — developer-focused content with technical keywords

        HTML rules:
        - Use realistic niche businesses or content (not lorem ipsum)
        - canonical URL must use https://example-eval.com/<slug>
        - Include <title>, <meta name="description">, <h1>, <link rel="canonical"> at minimum
        - og:title and og:description may or may not be present (vary this)
        - HTML should be 15–40 lines, realistic but minimal

        Return ONLY a valid JSON array. No text before or after the array. No markdown fences.
        """;

    public TestCaseGeneratorService(AnthropicHaikuClient haiku)
    {
        _haiku = haiku;
    }

    public async Task<IReadOnlyList<TestCase>> GenerateAsync(
        int count,
        string testCasesDir,
        CancellationToken ct = default)
    {
        var startId = GetNextId(testCasesDir);

        var userMessage = $$"""
            Generate exactly {{count}} test case(s). Start IDs from tc-{{startId:D3}}.

            Each object must match this exact JSON schema:
            {
              "id": "tc-NNN",
              "description": "one sentence describing the scenario type and niche",
              "fakePageHtml": "the full HTML string",
              "targetKeywords": ["keyword1", "keyword2"],
              "userMessage": "Please analyse https://example-eval.com/slug"
            }

            Return the JSON array only. No markdown, no explanation.
            """;

        Console.WriteLine($"Asking Haiku to generate {count} test case(s)...");
        var cases = await _haiku.CompleteJsonAsync<TestCase[]>(SystemPrompt, userMessage, ct);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        foreach (var tc in cases)
        {
            var path = Path.Combine(testCasesDir, $"{tc.Id}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(tc, options), ct);
            Console.WriteLine($"  Wrote {path}");
        }

        return cases;
    }

    private static int GetNextId(string dir)
    {
        if (!Directory.Exists(dir))
            return 1;

        var max = Directory.GetFiles(dir, "tc-*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Select(name => int.TryParse(name.Replace("tc-", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return max + 1;
    }
}
