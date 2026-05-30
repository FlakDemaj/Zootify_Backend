using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class TagEntity : BaseGuidEntity
{
    public string Tag { get; set; }
    
    public ICollection<RelationSpecimensTagEntity> SpecimensTags { get; set; }
}