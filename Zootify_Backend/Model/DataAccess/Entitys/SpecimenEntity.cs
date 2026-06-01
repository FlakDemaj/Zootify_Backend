using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class SpecimenEntity : BaseDeletionEntity
{
    public Guid TaxonomySpeciesGuid { get; set; }
    
    public TaxonomyNodeEntity Species { get; set; } = null!;
    
    public string Name { get; set; }
    
    public string LatinName  { get; set; }
    
    public DateTime DateOfDiscovery { get; set; }

    public Guid? GeographicLocationGuid { get; set; }
    
    public GeographicLocationEntity? GeographicLocation { get; set; }

    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    public DateTime? OnLoanUntil { get; set; }

    public string? StorageBuilding { get; set; }
    
    public string? StorageCabinet { get; set; }
    
    public string? StorageShelf { get; set; }
    
    public string? StorageCompartment { get; set; }

    public string? Comment { get; set; }

    public bool IsPublic { get; set; } = true;
    
    public ICollection<SpecimenImageEntity> Images { get; set; } = new List<SpecimenImageEntity>();
    
    public ICollection<LoanEntity> Loans { get; set; } = new List<LoanEntity>();
    
    public ICollection<RelationSpecimensTagEntity> SpecimensTags { get; set; }

    public void SetSpecies(List<TaxonomyNodeEntity> taxonomyNodes)
    {
        var species = 
            taxonomyNodes
                .OrderBy(x => x.Rank)
                .LastOrDefault();

        if (species == null)
        {
            throw new ZootifyException(ErrorCode.SpeciesNotFound);
        }

        TaxonomySpeciesGuid = species.Guid;
    }

    public void SetLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
