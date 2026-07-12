using Zootify_Backend.Model.DataAccess.DBContext;

namespace Zootify.Tests.Integration.Utils.Helpers;

/// <summary>
/// Specimen Test Repository — test support type.
/// </summary>
public class SpecimenTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public SpecimenTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Test support method: Create Taxonomy Node Async.
    /// </summary>
    public async Task<TaxonomyNodeEntity> CreateTaxonomyNodeAsync(
        string name,
        TaxonomyRank rank,
        ApprovalStatus approvalStatus = ApprovalStatus.Approved,
        Guid? parentGuid = null)
    {
        var node = new TaxonomyNodeEntity
        {
            Name = name,
            Rank = rank,
            ApprovalStatus = approvalStatus,
            ParentGuid = parentGuid
        };
        _dbContext.Add(node);
        await _dbContext.SaveChangesAsync();
        return node;
    }

    /// <summary>
    /// Test support method: Create Geographic Location Async.
    /// </summary>
    public async Task<GeographicLocationEntity> CreateGeographicLocationAsync(
        string name = "DE",
        GeographicLevel level = GeographicLevel.Country,
        Guid? parentGuid = null)
    {
        var location = new GeographicLocationEntity
        {
            Name       = name,
            Level      = level,
            ParentGuid = parentGuid,
        };
        _dbContext.Add(location);
        await _dbContext.SaveChangesAsync();
        return location;
    }

    /// <summary>
    /// Test support method: Create Specimen Async.
    /// </summary>
    public async Task<SpecimenEntity> CreateSpecimenAsync(
        Guid speciesGuid,
        Guid geoLocationGuid,
        SpecimenStatus status = SpecimenStatus.Active,
        bool isPublic = true,
        Guid? createdByUserGuid = null,
        string name = "Test Specimen",
        double? size = null,
        double? weight = null)
    {
        var specimen = new SpecimenEntity
        {
            Name = name,
            LatinName = "Testus specienus",
            TaxonomySpeciesGuid = speciesGuid,
            GeographicLocationGuid = geoLocationGuid,
            DateOfDiscovery = DateTime.UtcNow,
            Status = status,
            IsPublic = isPublic,
            Size = size,
            Weight = weight
        };
        if (createdByUserGuid.HasValue)
        {
            specimen.SetCreatedBy(createdByUserGuid.Value);
        }

        _dbContext.Add(specimen);
        await _dbContext.SaveChangesAsync();
        return specimen;
    }

    /// <summary>
    /// Test support method: Create Specimen With Image Async.
    /// </summary>
    public async Task<SpecimenEntity> CreateSpecimenWithImageAsync(
        Guid speciesGuid,
        Guid geoLocationGuid,
        string imageBase64 = "base64data",
        SpecimenStatus status = SpecimenStatus.Active,
        Guid? createdByUserGuid = null)
    {
        var specimen = await CreateSpecimenAsync(speciesGuid, geoLocationGuid, status, createdByUserGuid: createdByUserGuid);
        var image = new SpecimenImageEntity
        {
            ImageBase64 = imageBase64
        };
        _dbContext.Add(image);
        // The specimen owns the foreign key to the image.
        specimen.ImageGuid = image.Guid;
        await _dbContext.SaveChangesAsync();
        return specimen;
    }

    /// <summary>
    /// Test support method: Create Image For Specimen Async.
    /// </summary>
    public async Task<SpecimenImageEntity> CreateImageForSpecimenAsync(
        Guid speciesGuid,
        Guid geoLocationGuid,
        string imageBase64 = "base64data",
        SpecimenStatus status = SpecimenStatus.Active,
        Guid? createdByUserGuid = null)
    {
        var specimen = await CreateSpecimenAsync(speciesGuid, geoLocationGuid, status, createdByUserGuid: createdByUserGuid);
        var image = new SpecimenImageEntity
        {
            ImageBase64 = imageBase64
        };
        _dbContext.Add(image);
        // The specimen owns the foreign key to the image.
        specimen.ImageGuid = image.Guid;
        await _dbContext.SaveChangesAsync();
        return image;
    }

    
    /// <summary>
    /// Test support method: Create Specimen With Tags Async.
    /// </summary>
    public async Task<SpecimenEntity> CreateSpecimenWithTagsAsync(
        Guid speciesGuid,
        Guid geoLocationGuid,
        List<TagEntity> tags,
        SpecimenStatus status = SpecimenStatus.Active,
        Guid? createdByUserGuid = null)
    {
        var specimen = await CreateSpecimenAsync(speciesGuid, geoLocationGuid, status, createdByUserGuid: createdByUserGuid);
        foreach (var tag in tags)
        {
            _dbContext.Add(new RelationSpecimensTagEntity
            {
                SpecimenGuid = specimen.Guid,
                TagGuid = tag.Guid
            });
        }
        await _dbContext.SaveChangesAsync();
        return specimen;
    }
}
