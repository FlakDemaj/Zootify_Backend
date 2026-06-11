using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetTaxonomyTreeNodeDto
{
    public Guid TaxonomyGuid { get; set; }
    public required string Name { get; set; }
    public TaxonomyRank Rank { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public List<GetTaxonomyTreeNodeDto> Children { get; set; } = [];
}
