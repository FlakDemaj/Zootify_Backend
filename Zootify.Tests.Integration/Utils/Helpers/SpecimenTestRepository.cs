using Zootify_Backend.Model.DataAccess.DBContext;

namespace Zootify.Tests.Integration.Utils.Helpers;

public class SpecimenTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public SpecimenTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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

    public async Task<SpecimenEntity> CreateSpecimenAsync(
        Guid speciesGuid,
        Guid geoLocationGuid,
        SpecimenStatus status = SpecimenStatus.Active,
        bool isPublic = true,
        Guid? createdByUserGuid = null)
    {
        var specimen = new SpecimenEntity
        {
            Name = "Test Specimen",
            LatinName = "Testus specienus",
            TaxonomySpeciesGuid = speciesGuid,
            GeographicLocationGuid = geoLocationGuid,
            DateOfDiscovery = DateTime.UtcNow,
            Status = status,
            IsPublic = isPublic
        };
        if (createdByUserGuid.HasValue)
        {
            specimen.SetCreatedBy(createdByUserGuid.Value);
        }

        _dbContext.Add(specimen);
        await _dbContext.SaveChangesAsync();
        return specimen;
    }

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
            SpecimenGuid = specimen.Guid,
            ImageBase64 = imageBase64
        };
        _dbContext.Add(image);
        await _dbContext.SaveChangesAsync();
        return specimen;
    }

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
            SpecimenGuid = specimen.Guid,
            ImageBase64 = imageBase64
        };
        _dbContext.Add(image);
        await _dbContext.SaveChangesAsync();
        return image;
    }

    
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
