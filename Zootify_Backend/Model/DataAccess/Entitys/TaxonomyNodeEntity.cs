using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents a node in the taxonomy tree (e.g. phylum, class, species) with its approval state and hierarchy links.
/// </summary>
public class TaxonomyNodeEntity : BaseCreationEntity
{
    public string Name { get; set; } = string.Empty;
    
    public TaxonomyRank Rank { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    
    public Guid? ParentGuid { get; set; }

    public Guid? ApprovedByUserGuid { get; set; }

    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// When the node was rejected. Set when the node is moved to <see cref="ApprovalStatus.Rejected"/>;
    /// used by the purge job to hard-delete rejected nodes once the retention period has passed.
    /// </summary>
    public DateTime? RejectedDate { get; set; }

    public TaxonomyNodeEntity? Parent { get; set; }

    public UserEntity? ApprovedByUser { get; set; }

    public ICollection<TaxonomyNodeEntity> Children { get; set; } = new List<TaxonomyNodeEntity>();

    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
}
