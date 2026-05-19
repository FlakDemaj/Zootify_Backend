using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Entitys;

public class GeographicLocationEntity : BaseGuidEntity
{
    public string Name { get; set; } = string.Empty;
    
    public GeographicLevel Level { get; set; }

    public Guid? ParentGuid { get; set; }
   
    public GeographicLocationEntity? Parent { get; set; }
    
    public ICollection<GeographicLocationEntity> Children { get; set; } = new List<GeographicLocationEntity>();

    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
}
