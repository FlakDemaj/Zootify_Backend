using Zootify_Backend.Model.DataAccess.Entitys.Base;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Entitys;

public class SpecimenEntity : BaseDeletionEntity
{
    public Guid SpeciesGuid { get; set; }
    
    public TaxonomyNodeEntity Species { get; set; } = null!;

    public Guid? GeographicLocationGuid { get; set; }
    
    public GeographicLocationEntity? GeographicLocation { get; set; }

    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    public DateTime? FindDate { get; set; }

    public string? StorageBuilding { get; set; }
    
    public string? StorageCabinet { get; set; }
    
    public string? StorageShelf { get; set; }
    
    public string? StorageCompartment { get; set; }

    public string? Comment { get; set; }
    
    public bool IsPublic { get; set; }

    public ICollection<SpecimenImageEntity> Images { get; set; } = new List<SpecimenImageEntity>();
    
    public ICollection<LoanEntity> Loans { get; set; } = new List<LoanEntity>();
}
