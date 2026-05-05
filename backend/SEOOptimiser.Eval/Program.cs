using System.Diagnostics;
using System.Text.Json;
using SEOOptimiser.Eval.Infrastructure;
using SEOOptimiser.Eval.Models;
using SEOOptimiser.Eval.Services;

// Load .env from the project root (3 levels up from bin/Debug/net10.0/)
var envFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            continue;
        var eq = trimmed.IndexOf('=');
        if (eq <= 0) continue;
        var key = trimmed[..eq].Trim();
        var val = trimmed[(eq + 1)..].Trim().Trim('"');
        Environment.SetEnvironmentVariable(key, val);
    }
}

var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? throw new InvalidOperationException(
        "ANTHROPIC_API_KEY environment variable is required. " +
        $"Add it to {envFile} or export it in your shell.");

// Resolve project-root-relative dirs (3 levels up from bin/Debug/net10.0/)
var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var testCasesDir = Path.Combine(projectRoot, "test-cases");
var resultsDir   = Path.Combine(projectRoot, "results");
Directory.CreateDirectory(testCasesDir);
Directory.CreateDirectory(resultsDir);

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = args[0].ToLowerInvariant();

if (command == "generate")
{
    var count = ParseInt(args, "--count", defaultValue: 5, max: 20);
    using var haiku = new AnthropicHaikuClient(apiKey);
    var generator = new TestCaseGeneratorService(haiku);
    await generator.GenerateAsync(count, testCasesDir);
    Console.WriteLine($"\nDone. {count} test case(s) written to {testCasesDir}");
    return 0;
}

if (command == "evaluate")
{
    var specificId = ParseString(args, "--test-case-id");

    IReadOnlyList<TestCase> testCases;
    if (specificId is not null)
    {
        var file = Path.Combine(testCasesDir, $"{specificId}.json");
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"Test case file not found: {file}");
            return 2;
        }
        testCases = [LoadTestCase(file)];
    }
    else
    {
        testCases = Directory.GetFiles(testCasesDir, "tc-*.json")
            .OrderBy(f => f)
            .Select(LoadTestCase)
            .ToList();

        if (testCases.Count == 0)
        {
            Console.Error.WriteLine(
                $"No test cases found in {testCasesDir}. Run 'generate' first.");
            return 2;
        }
    }

    Console.WriteLine($"Evaluating {testCases.Count} test case(s)...\n");

    using var haiku = new AnthropicHaikuClient(apiKey);
    var runner    = new AgentRunnerService(apiKey);
    var evaluator = new EvaluatorService(haiku);

    var scores = new List<int>();
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    foreach (var tc in testCases)
    {
        Console.WriteLine($"[{tc.Id}] {tc.Description}");
        var sw = Stopwatch.StartNew();

        try
        {
            Console.Write("  Running agent... ");
            var agentOutput = await runner.RunAsync(tc);
            Console.WriteLine($"done ({agentOutput.Suggestions.Count} suggestions)");

            Console.Write("  Scoring with Haiku... ");
            var score = await evaluator.ScoreAsync(tc, agentOutput);
            sw.Stop();

            Console.WriteLine($"done ({sw.Elapsed.TotalSeconds:F1}s)");
            Console.WriteLine($"  Score:      {score.Score}/100");
            Console.WriteLine($"  Reasoning:  {score.Reasoning}");
            Console.WriteLine($"  Strengths:  {string.Join(", ", score.Strengths)}");
            Console.WriteLine($"  Weaknesses: {string.Join(", ", score.Weaknesses)}");

            var result = new EvalResult(
                TestCaseId:    tc.Id,
                Description:   tc.Description,
                TargetKeywords: tc.TargetKeywords,
                AgentOutput:   agentOutput,
                Score:         score,
                RanAt:         DateTime.UtcNow);

            var resultPath = Path.Combine(resultsDir, $"{tc.Id}-result.json");
            await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(result, jsonOptions));
            Console.WriteLine($"  Result written to {resultPath}\n");

            scores.Add(score.Score);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.Error.WriteLine($"  ERROR ({sw.Elapsed.TotalSeconds:F1}s): {ex.Message}\n");
        }
    }

    if (scores.Count > 0)
    {
        Console.WriteLine("─────────────────────────────────");
        Console.WriteLine($"Results: {scores.Count}/{testCases.Count} evaluated");
        Console.WriteLine($"Average: {scores.Average():F1}/100");
        Console.WriteLine($"Min:     {scores.Min()}/100");
        Console.WriteLine($"Max:     {scores.Max()}/100");
        Console.WriteLine("─────────────────────────────────");
    }

    return 0;
}

Console.Error.WriteLine($"Unknown command: {command}");
PrintUsage();
return 1;

static void PrintUsage()
{
    Console.WriteLine("""
        SEO Agent Evaluator

        Usage:
          dotnet run -- generate [--count N]               Generate test cases (default N=5, max 20)
          dotnet run -- evaluate [--test-case-id tc-NNN]   Evaluate all or a specific test case

        Environment:
          ANTHROPIC_API_KEY   Required. Your Anthropic API key.
        """);
}

static int ParseInt(string[] args, string flag, int defaultValue, int max)
{
    var idx = Array.IndexOf(args, flag);
    if (idx >= 0 && idx + 1 < args.Length && int.TryParse(args[idx + 1], out var val))
        return Math.Min(val, max);
    return defaultValue;
}

static string? ParseString(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static TestCase LoadTestCase(string path)
{
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<TestCase>(json, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    }) ?? throw new InvalidOperationException($"Failed to parse test case: {path}");
}
