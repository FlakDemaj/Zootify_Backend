using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request;

public class CreateTaxonomyNodeRequestDto
{
    public Guid? ParentGuid { get; init; }
    
    public required Dictionary<TaxonomyRank, string> TaxonomyNodes { get; init; }
}