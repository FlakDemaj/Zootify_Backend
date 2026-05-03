using Zootify_Backend.Enums;

namespace Zootify_Backend.Models;

public class TaxonomyNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TaxonomyRank Rank { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved;

    public int? ParentId { get; set; }
    public TaxonomyNode? Parent { get; set; }
    public ICollection<TaxonomyNode> Children { get; set; } = new List<TaxonomyNode>();

    public int? ProposedByUserId { get; set; }
    public User? ProposedByUser { get; set; }

    public ICollection<Specimen> Specimens { get; set; } = new List<Specimen>();
}
