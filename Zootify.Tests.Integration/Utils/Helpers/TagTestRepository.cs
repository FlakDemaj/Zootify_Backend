namespace Zootify.Tests.Integration.Utils.Helpers;

public class TagTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public TagTestRepository(ZootifyDbContext dbContext)
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
}