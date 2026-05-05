namespace SEOOptimiser.Eval.Models;

public record TestCase(
    string Id,
    string Description,
    string FakePageHtml,
    string[] TargetKeywords,
    string UserMessage
);
