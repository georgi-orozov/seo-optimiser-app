namespace SEOOptimiser.Eval.Models;

public record SuggestionRecord(
    string Tag,
    string? CurrentValue,
    string SuggestedValue
);

public record AgentOutput(
    string Text,
    IReadOnlyList<SuggestionRecord> Suggestions
);

public record EvalScore(
    int Score,
    string Reasoning,
    string[] Strengths,
    string[] Weaknesses
);

public record EvalResult(
    string TestCaseId,
    string Description,
    string[] TargetKeywords,
    AgentOutput AgentOutput,
    EvalScore Score,
    DateTime RanAt
);
