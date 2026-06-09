using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request;

public class CreateTaxonomyNodeRequestDto
{
    public Guid? ParentGuid { get; set; }
    
    public required Dictionary<TaxonomyRank, string> TaxonomyNodes { get; set; }
}