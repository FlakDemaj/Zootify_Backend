using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllTaxonomyNodesByFilterDto
{
    public required Guid TaxonomyGuid { get; init; }
    
    public required string Name { get; init; }
    
    public required TaxonomyRank Rank { get; init; }
    
    public required ApprovalStatus ApprovalStatus { get; init; }
}