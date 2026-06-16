using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for creating one or more taxonomy nodes, optionally beneath an existing parent.
/// </summary>
public class CreateTaxonomyNodeRequestDto
{
    /// <summary>The GUID of the parent node to attach to, or <c>null</c> to create a new root chain.</summary>
    public Guid? ParentGuid { get; init; }

    /// <summary>The node name to create for each taxonomy rank in the chain.</summary>
    public required Dictionary<TaxonomyRank, string> TaxonomyNodes { get; init; }
}
