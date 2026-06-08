using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class SpecimenCollectionEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }
    
    public Guid CollectionGuid { get; set; }
    
    public SpecimenEntity Specimen { get; set; }
    
    public CollectionEntity Collection { get; set; }
}