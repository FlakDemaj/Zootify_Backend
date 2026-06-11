using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class SpecimenEntity : BaseDeletionEntity
{
    public Guid? TaxonomySpeciesGuid { get; set; }

    public TaxonomyNodeEntity? Species { get; set; }
    
    public required string Name { get; set; }
    
    public required string LatinName  { get; set; }
    
    public DateTime DateOfDiscovery { get; set; }

    public Guid GeographicLocationGuid { get; set; }
    
    public GeographicLocationEntity GeographicLocation { get; set; }

    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    public string? StorageBuilding { get; set; }
    
    public string? StorageCabinet { get; set; }
    
    public string? StorageShelf { get; set; }
    
    public string? StorageCompartment { get; set; }

    public string? Comment { get; set; }

    public bool IsPublic { get; set; } = true;

    public SpecimenStatus Status { get; set; } = SpecimenStatus.Active;

    public DateTime? RejectedAt { get; set; }
    
    public SpecimenImageEntity? Image { get; set; }
    
    public ICollection<LoanEntity> Loans { get; set; } = new List<LoanEntity>();
    
    public ICollection<RelationSpecimensTagEntity> SpecimensTags { get; set; } = new  List<RelationSpecimensTagEntity>();
    
    public ICollection<SpecimenCollectionEntity> Collections { get; set; } = new List<SpecimenCollectionEntity>();

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

        TaxonomySpeciesGuid = (Guid?)species.Guid;
    }

    public void SetLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public bool CheckChanges(SpecimenEntity newSpecimenData)
    {
        return Name != newSpecimenData.Name
               || DateOfDiscovery != newSpecimenData.DateOfDiscovery
               || LatinName != newSpecimenData.LatinName
               || Comment != newSpecimenData.Comment
               || IsPublic != newSpecimenData.IsPublic
               || Status != newSpecimenData.Status;

    }
    
    public void UpdateData(
        Guid requester,
        SpecimenEntity newSpecimenData)
    {
        Name = newSpecimenData.Name;
        DateOfDiscovery = newSpecimenData.DateOfDiscovery;
        Latitude = newSpecimenData.Latitude; 
        LatinName = newSpecimenData.LatinName;
        Comment = newSpecimenData.Comment;
        IsPublic = newSpecimenData.IsPublic;
        Status = newSpecimenData.Status;
        SetUpdatedBy(requester);

    }

    public void CreateImage(string newImage, Guid requester)
    {
        Image = new SpecimenImageEntity
        {
            SpecimenGuid = Guid,
            ImageBase64 = newImage,
        };

        SetUpdatedBy(requester);
    }
}
