using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request.Helpers;

public class TaxonomyDto
{
    public Guid? Guid { get; set; }
    
    public required string Name { get; set; }
    
    public TaxonomyRank Rank { get; set; }
}