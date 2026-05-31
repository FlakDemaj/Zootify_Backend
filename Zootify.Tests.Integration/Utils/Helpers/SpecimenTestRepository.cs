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
}
