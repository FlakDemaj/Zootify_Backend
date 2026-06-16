using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A single taxonomy node entry returned by filtered taxonomy queries.
/// </summary>
public class GetAllTaxonomyNodesByFilterDto
{
    /// <summary>The taxonomy node's GUID.</summary>
    public required Guid TaxonomyGuid { get; init; }

    /// <summary>The node's name.</summary>
    public required string Name { get; init; }

    /// <summary>The node's biological rank.</summary>
    public required TaxonomyRank Rank { get; init; }

    /// <summary>The node's moderation/approval status.</summary>
    public required ApprovalStatus ApprovalStatus { get; init; }
}
