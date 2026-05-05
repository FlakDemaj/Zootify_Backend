using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs;

public class CreateTaxonomyNodeDto
{
    public string Name { get; set; } = string.Empty;
    public TaxonomyRank Rank { get; set; }
    public int? ParentId { get; set; }
    public int? ProposedByUserId { get; set; }
}
