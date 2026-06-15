using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The taxonomy node currently under review together with existing taxonomies of the same rank whose
/// name sounds similar, ordered by descending similarity.
/// </summary>
public class GetSimilarTaxonomiesResponseDto
{
    public Guid TaxonomyGuid { get; init; }
    public required string Name { get; init; }
    public TaxonomyRank Rank { get; init; }
    public List<SimilarTaxonomyDto> Similar { get; init; } = [];
}
