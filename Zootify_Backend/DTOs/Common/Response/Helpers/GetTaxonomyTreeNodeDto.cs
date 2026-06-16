using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A node in the taxonomy tree, with its nested child nodes.
/// </summary>
public class GetTaxonomyTreeNodeDto
{
    /// <summary>The taxonomy node's GUID.</summary>
    public Guid TaxonomyGuid { get; init; }

    /// <summary>The node's name.</summary>
    public required string Name { get; init; }

    /// <summary>The node's biological rank.</summary>
    public TaxonomyRank Rank { get; init; }

    /// <summary>The node's moderation/approval status.</summary>
    public ApprovalStatus ApprovalStatus { get; init; }

    /// <summary>The node's child nodes (next rank down).</summary>
    public List<GetTaxonomyTreeNodeDto> Children { get; init; } = [];
}
