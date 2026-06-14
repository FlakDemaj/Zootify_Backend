using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents a node in the hierarchical geographic location tree (e.g. country, state, city, street).
/// </summary>
public class GeographicLocationEntity : BaseGuidEntity
{
    public string Name { get; set; } = string.Empty;
    
    public GeographicLevel Level { get; set; }

    public Guid? ParentGuid { get; set; }
   
    public GeographicLocationEntity? Parent { get; set; }
    
    public ICollection<GeographicLocationEntity> Children { get; set; } = new List<GeographicLocationEntity>();

    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
}
