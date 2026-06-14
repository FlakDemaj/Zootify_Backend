using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Join entity linking a specimen to a tag (many-to-many relationship).
/// </summary>
public class RelationSpecimensTagEntity : BaseGuidEntity
{
    public Guid TagGuid { get; set; }
    
    public Guid SpecimenGuid { get; set; }
    
    public TagEntity Tag { get; set; }
    
    public SpecimenEntity Specimen { get; set; }
}