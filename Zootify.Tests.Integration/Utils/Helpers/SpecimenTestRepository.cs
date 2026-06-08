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
        GeographicLevel level = GeographicLevel.Country)
    {
        var location = new GeographicLocationEntity
        {
            Name = name,
            Level = level
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
}
