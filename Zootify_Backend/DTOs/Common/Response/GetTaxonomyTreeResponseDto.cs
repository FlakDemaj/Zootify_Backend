using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The (possibly filtered) taxonomy tree together with the total node count.
/// </summary>
public class GetTaxonomyTreeResponseDto
{
    /// <summary>The total number of matching nodes.</summary>
    public int TotalCount { get; init; }

    /// <summary>The root nodes of the taxonomy tree (with nested children).</summary>
    public List<GetTaxonomyTreeNodeDto> Nodes { get; init; } = [];
}
