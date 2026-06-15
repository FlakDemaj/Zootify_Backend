using FluentAssertions;
using Xunit;
using Zootify_Backend;

namespace Zootify.Tests.Unit.Utils;

public class FuzzyMatchTests
{
    [Fact]
    public void Similarity_EqualStrings_ReturnsOne()
    {
        FuzzyMatch.Similarity("Papilio machaon", "Papilio machaon").Should().Be(1.0);
    }

    [Fact]
    public void Similarity_IsCaseInsensitiveAndTrimmed()
    {
        FuzzyMatch.Similarity("Papilio machaon", "  papilio MACHAON ").Should().Be(1.0);
    }

    [Fact]
    public void Similarity_SmallTypo_IsHigh()
    {
        // One missing character out of 15.
        FuzzyMatch.Similarity("Papilio machaon", "Papilio machao").Should().BeGreaterThan(0.9);
    }

    [Fact]
    public void Similarity_DifferentStrings_IsLow()
    {
        FuzzyMatch.Similarity("Papilio machaon", "Carabus coriaceus").Should().BeLessThan(0.5);
    }

    [Fact]
    public void BestMatch_ReturnsClosestCandidateAboveThreshold()
    {
        var candidates = new[] { "Carabus coriaceus", "Papilio machaon", "Vanessa atalanta" };

        var match = FuzzyMatch.BestMatch("Papilio machao", candidates, c => c, threshold: 0.7);

        match.Should().Be("Papilio machaon");
    }

    [Fact]
    public void BestMatch_NoCandidateReachesThreshold_ReturnsNull()
    {
        var candidates = new[] { "Carabus coriaceus", "Vanessa atalanta" };

        var match = FuzzyMatch.BestMatch("Papilio machaon", candidates, c => c, threshold: 0.7);

        match.Should().BeNull();
    }
}
