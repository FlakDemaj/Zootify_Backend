using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents a collected specimen including its taxonomy, location, storage details, image, and status.
/// </summary>
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

    public double? Size { get; set; }

    public double? Weight { get; set; }

    public bool IsPublic { get; set; } = true;

    public SpecimenStatus Status { get; set; } = SpecimenStatus.Active;

    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// Reference to the specimen's image. The image content itself is loaded separately by this GUID,
    /// so the (potentially large) image row does not have to be fetched together with the specimen.
    /// </summary>
    public Guid? ImageGuid { get; set; }

    public SpecimenImageEntity? Image { get; set; }
    
    public ICollection<LoanEntity> Loans { get; set; } = new List<LoanEntity>();

    public ICollection<LoanRequestEntity> LoanRequests { get; set; } = new List<LoanRequestEntity>();
    
    public ICollection<RelationSpecimensTagEntity> SpecimensTags { get; set; } = new  List<RelationSpecimensTagEntity>();
    
    public ICollection<SpecimenCollectionEntity> Collections { get; set; } = new List<SpecimenCollectionEntity>();

    /// <summary>
    /// Sets the species reference to the deepest (highest-rank) node in the given taxonomy chain.
    /// </summary>
    /// <param name="taxonomyNodes">The taxonomy chain to derive the species from.</param>
    /// <exception cref="ZootifyException">Thrown when the chain contains no node to use as the species.</exception>
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

    /// <summary>
    /// Soft-deletes the specimen by setting its status to deleted, recording the requester and set the
    /// Specimen to private.
    /// </summary>
    /// <param name="requesterGuid">The GUID of the user performing the deletion.</param>
    public void DeleteSpecimen(Guid requesterGuid)
    {
        Status = SpecimenStatus.Deleted;
        IsPublic = false;
        SetDeletedBy(requesterGuid);
    }

    /// <summary>
    /// Restores a soft-deleted specimen to active status and records the requester as the updater.
    /// </summary>
    /// <param name="requesterGuid">The GUID of the user performing the activation.</param>
    public void ActivateSpecimen(Guid requesterGuid)
    {
        Status = SpecimenStatus.Active;
        RemoveDeletion();
        SetUpdatedBy(requesterGuid);
    }

    /// <summary>
    /// Sets the specimen's geographic coordinates.
    /// </summary>
    /// <param name="latitude">The latitude to set.</param>
    /// <param name="longitude">The longitude to set.</param>
    public void SetLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Determines whether the editable specimen fields differ from the given data.
    /// </summary>
    /// <param name="newSpecimenData">The candidate data to compare against the current values.</param>
    /// <returns><c>true</c> if name, discovery date, latin name, comment, visibility, size, weight, or any storage field differ; otherwise <c>false</c>.</returns>
    public bool CheckChanges(SpecimenEntity newSpecimenData)
    {
        return Name != newSpecimenData.Name
               || DateOfDiscovery != newSpecimenData.DateOfDiscovery
               || LatinName != newSpecimenData.LatinName
               || Comment != newSpecimenData.Comment
               || IsPublic != newSpecimenData.IsPublic
               || Size != newSpecimenData.Size
               || Weight != newSpecimenData.Weight
               || StorageBuilding != newSpecimenData.StorageBuilding
               || StorageCabinet != newSpecimenData.StorageCabinet
               || StorageShelf != newSpecimenData.StorageShelf
               || StorageCompartment != newSpecimenData.StorageCompartment;

    }

    /// <summary>
    /// Applies the given specimen data to this entity and records the requester as the updater.
    /// </summary>
    /// <param name="requester">The GUID of the user performing the update.</param>
    /// <param name="newSpecimenData">The new data to apply.</param>
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
        Size = newSpecimenData.Size;
        Weight = newSpecimenData.Weight;
        StorageBuilding = newSpecimenData.StorageBuilding;
        StorageCabinet = newSpecimenData.StorageCabinet;
        StorageShelf = newSpecimenData.StorageShelf;
        StorageCompartment = newSpecimenData.StorageCompartment;
        SetUpdatedBy(requester);

    }

    /// <summary>
    /// Attaches a new Base64-encoded image to the specimen and records the requester as the updater.
    /// </summary>
    /// <param name="newImage">The Base64-encoded image content.</param>
    /// <param name="requester">The GUID of the user performing the update.</param>
    public void CreateImage(string newImage, Guid requester)
    {
        // The specimen holds the foreign key (ImageGuid). The image GUID is generated in-app, so set
        // both the navigation and the reference to keep the entity consistent before it is saved.
        Image = new SpecimenImageEntity
        {
            ImageBase64 = newImage,
        };
        ImageGuid = Image.Guid;

        SetUpdatedBy(requester);
    }
}
