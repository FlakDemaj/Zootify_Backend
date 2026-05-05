using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs;

public class TaxonomyNodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TaxonomyRank Rank { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int? ProposedByUserId { get; set; }
    public List<TaxonomyNodeDto> Children { get; set; } = [];
}
