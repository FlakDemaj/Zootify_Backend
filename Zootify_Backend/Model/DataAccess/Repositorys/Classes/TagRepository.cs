using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class TagRepository : BaseRepository<TagEntity>, ITagRepository
{
    public TagRepository(ZootifyDbContext context) : base(context)
    {
    }

    public Task<List<TagEntity>> GetTagByGuidAsync(List<Guid> guids)
    {
        return Context
            .GetTrackingSet<TagEntity>()
            .Where(t => guids.Contains(t.Guid))
            .ToListAsync();
    }

    public Task<List<TagEntity>> GetTagsByStartsWithNameAsync(string? startsWith)
    {
        var getAllTagsQuery =  Context
            .GetTrackingSet<TagEntity>();

        if (!string.IsNullOrWhiteSpace(startsWith))
        {
            getAllTagsQuery = getAllTagsQuery
                .Where(t => t.Tag.StartsWith(startsWith));
        }
        
        return getAllTagsQuery.ToListAsync();
    }

    public async Task<List<TagEntity>> GetTagsByNameAsync(List<string> names)
    {
        return await Context.GetTrackingSet<TagEntity>()
            .Where(t => names.Contains(t.Tag))
            .ToListAsync();
    }
}