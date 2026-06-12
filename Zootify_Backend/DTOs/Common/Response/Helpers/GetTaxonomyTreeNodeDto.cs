using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response.Helpers;

public class GetTaxonomyTreeNodeDto
{
    public Guid TaxonomyGuid { get; init; }
    public required string Name { get; init; }
    public TaxonomyRank Rank { get; init; }
    public ApprovalStatus ApprovalStatus { get; init; }
    public List<GetTaxonomyTreeNodeDto> Children { get; init; } = [];
}
