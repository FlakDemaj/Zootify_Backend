using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A single existing taxonomy node whose name sounds similar to the one under review, including its
/// similarity score and ancestor path to help a moderator decide whether it is a duplicate.
/// </summary>
public class SimilarTaxonomyDto
{
    public Guid TaxonomyGuid { get; init; }
    public required string Name { get; init; }
    public TaxonomyRank Rank { get; init; }
    public ApprovalStatus ApprovalStatus { get; init; }

    /// <summary>The normalized name similarity (0..1) to the reviewed taxonomy; 1 means identical.</summary>
    public double Similarity { get; init; }

    /// <summary>The ancestor path from the root to this node's parent, e.g. "Animalia &gt; Chordata".</summary>
    public required string Path { get; init; }
}
