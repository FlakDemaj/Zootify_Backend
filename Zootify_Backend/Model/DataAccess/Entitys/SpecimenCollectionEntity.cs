using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Join entity linking a specimen to a collection (many-to-many relationship).
/// </summary>
public class SpecimenCollectionEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }
    
    public Guid CollectionGuid { get; set; }
    
    public SpecimenEntity Specimen { get; set; }
    
    public CollectionEntity Collection { get; set; }
}