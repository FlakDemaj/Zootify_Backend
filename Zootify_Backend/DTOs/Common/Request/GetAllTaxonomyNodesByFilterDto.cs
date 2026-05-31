using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request;

public class GetAllTaxonomyNodesByFilterDto
{
    public required Guid TaxonomyGuid { get; set; }
    
    public required string Name { get; set; }
    
    public required TaxonomyRank Rank { get; set; }
}