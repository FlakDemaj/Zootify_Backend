using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllTaxonomyNodesByFilterDto
{
    public required Guid TaxonomyGuid { get; init; }
    
    public required string Name { get; init; }
    
    public required TaxonomyRank Rank { get; init; }
    
    public required ApprovalStatus ApprovalStatus { get; init; }
    
    public static List<GetAllTaxonomyNodesByFilterDto> DeletedTaxonomys() =>
       [ 
           new()
            {
                TaxonomyGuid = Guid.Empty,
                Name = "Taxonomy deleted due to rejection",
                Rank = TaxonomyRank.Species,
                ApprovalStatus = ApprovalStatus.Rejected
            }
       ];
}