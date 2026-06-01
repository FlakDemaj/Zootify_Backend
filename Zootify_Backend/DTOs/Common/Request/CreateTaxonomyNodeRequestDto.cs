namespace Zootify_Backend.DTOs.Common.Request;

public class CreateTaxonomyNodeRequestDto
{
    public Guid? ParentGuid { get; set; }
    
    public required string TaxonomyName { get; set; }
}