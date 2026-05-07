using FluentAssertions;
using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Core.Tests.Entities;

public class SuggestionTest
{
    [Fact]
    public void Create_WithValidArguments_SetsNonEmptyId()
    {
        var suggestion = Suggestion.Create(Guid.NewGuid(), "title", "Old Title", "New Title");

        suggestion.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithValidArguments_SetsMessageId()
    {
        var messageId = Guid.NewGuid();
        var suggestion = Suggestion.Create(messageId, "title", "Old", "New");

        suggestion.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void Create_WithValidArguments_SetsTag()
    {
        var suggestion = Suggestion.Create(Guid.NewGuid(), "meta description", null, "New Description");

        suggestion.Tag.Should().Be("meta description");
    }

    [Fact]
    public void Create_WithNullCurrentValue_SetsCurrentValueNull()
    {
        var suggestion = Suggestion.Create(Guid.NewGuid(), "h1", null, "New H1");

        suggestion.CurrentValue.Should().BeNull();
    }

    [Fact]
    public void Create_WithNonNullCurrentValue_SetsCurrentValue()
    {
        var suggestion = Suggestion.Create(Guid.NewGuid(), "title", "Old Title", "New Title");

        suggestion.CurrentValue.Should().Be("Old Title");
    }

    [Fact]
    public void Create_WithValidArguments_SetsSuggestedValue()
    {
        var suggestion = Suggestion.Create(Guid.NewGuid(), "title", "Old Title", "New Title");

        suggestion.SuggestedValue.Should().Be("New Title");
    }

    [Fact]
    public void Create_MultipleCallsWithSameArguments_ProduceDifferentIds()
    {
        var messageId = Guid.NewGuid();
        var s1 = Suggestion.Create(messageId, "title", null, "New");
        var s2 = Suggestion.Create(messageId, "title", null, "New");

        s1.Id.Should().NotBe(s2.Id);
    }
}
