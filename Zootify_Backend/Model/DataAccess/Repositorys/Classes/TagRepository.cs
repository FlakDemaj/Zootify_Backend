using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

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
}