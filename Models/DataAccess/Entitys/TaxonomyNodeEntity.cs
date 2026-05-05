using Zootify_Backend.Enums;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Entitys;

public class TaxonomyNodeEntity: BaseGuidEntity
{
    public string Name { get; set; } = string.Empty;
    
    public TaxonomyRank Rank { get; set; }
    
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved;
    
    public Guid? ParentGuid { get; set; }
    
    public TaxonomyNodeEntity? Parent { get; set; }
    
    public ICollection<TaxonomyNodeEntity> Children { get; set; } = new List<TaxonomyNodeEntity>();
    
    public Guid? ProposedByUserGuid { get; set; }

    public UserEntity? ProposedByUser { get; set; }

    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
}
