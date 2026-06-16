using Zootify_Backend.Model.DataAccess.DBContext;

namespace Zootify.Tests.Integration.Utils.Helpers;

/// <summary>
/// Tag Test Repository — test support type.
/// </summary>
public class TagTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public TagTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    /// <summary>
    /// Test support method: Create Tag Async.
    /// </summary>
    public async Task<TagEntity> CreateTagAsync(string tagValue)
    {
        var tag = new TagEntity { Tag = tagValue };
        _dbContext.Add(tag);
        await _dbContext.SaveChangesAsync();
        return tag;
    }
}