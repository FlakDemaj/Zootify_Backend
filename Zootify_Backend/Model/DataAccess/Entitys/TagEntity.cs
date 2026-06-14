using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents a tag that can be associated with specimens.
/// </summary>
public class TagEntity : BaseGuidEntity
{
    public required string Tag { get; set; }
    
    public ICollection<RelationSpecimensTagEntity> SpecimensTags { get; set; } = new List<RelationSpecimensTagEntity>();
}