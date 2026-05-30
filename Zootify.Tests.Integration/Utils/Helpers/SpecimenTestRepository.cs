using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify.Tests.Integration.Utils.Helpers;

public class SpecimenTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public SpecimenTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TagEntity> CreateTagAsync(string tagValue)
    {
        var tag = new TagEntity { Tag = tagValue };
        _dbContext.Add(tag);
        await _dbContext.SaveChangesAsync();
        return tag;
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
