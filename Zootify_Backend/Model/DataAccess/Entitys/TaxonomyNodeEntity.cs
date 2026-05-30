using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class TaxonomyNodeEntity: BaseDeletionEntity
{
    public string Name { get; set; } = string.Empty;
    
    public TaxonomyRank Rank { get; set; }
    
    public ApprovalStatus ApprovalStatus { get; set; }
    
    public Guid? ParentGuid { get; set; }
    
    public TaxonomyNodeEntity? Parent { get; set; }
    
    public ICollection<TaxonomyNodeEntity> Children { get; set; } = new List<TaxonomyNodeEntity>();

    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
}
