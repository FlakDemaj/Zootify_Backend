using FluentAssertions;
using Xunit;
using Zootify_Backend;

namespace Zootify.Tests.Unit.Utils;

/// <summary>
/// Tests for Fuzzy Match.
/// </summary>
public class FuzzyMatchTests
{
    /// <summary>
    /// Tests Similarity – Equal Strings – Returns One.
    /// </summary>
    [Fact]
    public void Similarity_EqualStrings_ReturnsOne()
    {
        FuzzyMatch.Similarity("Papilio machaon", "Papilio machaon").Should().Be(1.0);
    }

    /// <summary>
    /// Tests Similarity – Is Case Insensitive And Trimmed.
    /// </summary>
    [Fact]
    public void Similarity_IsCaseInsensitiveAndTrimmed()
    {
        FuzzyMatch.Similarity("Papilio machaon", "  papilio MACHAON ").Should().Be(1.0);
    }

    /// <summary>
    /// Tests Similarity – Small Typo – Is High.
    /// </summary>
    [Fact]
    public void Similarity_SmallTypo_IsHigh()
    {
        // One missing character out of 15.
        FuzzyMatch.Similarity("Papilio machaon", "Papilio machao").Should().BeGreaterThan(0.9);
    }

    /// <summary>
    /// Tests Similarity – Different Strings – Is Low.
    /// </summary>
    [Fact]
    public void Similarity_DifferentStrings_IsLow()
    {
        FuzzyMatch.Similarity("Papilio machaon", "Carabus coriaceus").Should().BeLessThan(0.5);
    }

    /// <summary>
    /// Tests Best Match – Returns Closest Candidate Above Threshold.
    /// </summary>
    [Fact]
    public void BestMatch_ReturnsClosestCandidateAboveThreshold()
    {
        var candidates = new[] { "Carabus coriaceus", "Papilio machaon", "Vanessa atalanta" };

        var match = FuzzyMatch.BestMatch("Papilio machao", candidates, c => c, threshold: 0.7);

        match.Should().Be("Papilio machaon");
    }

    /// <summary>
    /// Tests Best Match – No Candidate Reaches Threshold – Returns Null.
    /// </summary>
    [Fact]
    public void BestMatch_NoCandidateReachesThreshold_ReturnsNull()
    {
        var candidates = new[] { "Carabus coriaceus", "Vanessa atalanta" };

        var match = FuzzyMatch.BestMatch("Papilio machaon", candidates, c => c, threshold: 0.7);

        match.Should().BeNull();
    }

    /// <summary>
    /// Tests Search Augmented – Empty Query – Returns All Ordered By Name.
    /// </summary>
    [Fact]
    public void SearchAugmented_EmptyQuery_ReturnsAllOrderedByName()
    {
        var items = new[] { "Vanessa atalanta", "Carabus coriaceus", "Papilio machaon" };

        var result = FuzzyMatch.SearchAugmented(null, items, c => c);

        result.Should().ContainInOrder("Carabus coriaceus", "Papilio machaon", "Vanessa atalanta");
    }

    /// <summary>
    /// Tests Search Augmented – Includes Substring Match.
    /// </summary>
    [Fact]
    public void SearchAugmented_IncludesSubstringMatch()
    {
        var items = new[] { "Carabus coriaceus", "Papilio machaon" };

        var result = FuzzyMatch.SearchAugmented("machaon", items, c => c);

        result.Should().ContainSingle().Which.Should().Be("Papilio machaon");
    }

    /// <summary>
    /// Tests Search Augmented – Includes Typo Via Fuzzy.
    /// </summary>
    [Fact]
    public void SearchAugmented_IncludesTypoViaFuzzy()
    {
        var items = new[] { "Carabus coriaceus", "Panthera leo" };

        // "leoo" is not a substring of any name, but is a close typo of the word "leo".
        var result = FuzzyMatch.SearchAugmented("Panthera leoo", items, c => c);

        result.Should().ContainSingle().Which.Should().Be("Panthera leo");
    }

    /// <summary>
    /// Tests Search Augmented – Orders Substring Matches Before Fuzzy Only.
    /// </summary>
    [Fact]
    public void SearchAugmented_OrdersSubstringMatchesBeforeFuzzyOnly()
    {
        var items = new[] { "Pantheraa", "Panthera leo" };

        // "Panthera leo" contains the query (substring) → ranked first; "Pantheraa" is only a fuzzy hit.
        var result = FuzzyMatch.SearchAugmented("Panthera l", items, c => c);

        result[0].Should().Be("Panthera leo");
    }

    /// <summary>
    /// Tests Search Augmented – No Match – Returns Empty.
    /// </summary>
    [Fact]
    public void SearchAugmented_NoMatch_ReturnsEmpty()
    {
        var items = new[] { "Carabus coriaceus", "Panthera leo" };

        var result = FuzzyMatch.SearchAugmented("zzznomatch", items, c => c);

        result.Should().BeEmpty();
    }
}
