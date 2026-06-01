using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class RelationSpecimensTagEntity : BaseGuidEntity
{
    public Guid TagGuid { get; set; }
    
    public Guid SpecimenGuid { get; set; }
    
    public TagEntity Tag { get; set; }
    
    public SpecimenEntity Specimen { get; set; }
}