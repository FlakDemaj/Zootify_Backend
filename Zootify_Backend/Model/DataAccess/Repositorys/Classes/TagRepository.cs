using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="TagEntity"/> records.
/// </summary>
public class TagRepository : BaseRepository<TagEntity>, ITagRepository
{
    /// <summary>
    /// Initializes a new <see cref="TagRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public TagRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns all tags whose GUIDs are in the given list.
    /// </summary>
    /// <param name="guids">The list of tag GUIDs to retrieve.</param>
    /// <returns>The matching <see cref="TagEntity"/> records.</returns>
    public Task<List<TagEntity>> GetTagByGuidAsync(List<Guid> guids)
    {
        return Context
            .GetTrackingSet<TagEntity>()
            .Where(t => guids.Contains(t.Guid))
            .ToListAsync();
    }

    /// <summary>
    /// Returns all tags whose name contains the given fragment, or all tags if no filter is provided.
    /// </summary>
    /// <param name="filter">Optional name fragment to filter tag names.</param>
    /// <returns>The matching <see cref="TagEntity"/> records.</returns>
    public Task<List<TagEntity>> GetTagsByNameFilterAsync(string? filter)
    {
        var getAllTagsQuery =  Context
            .GetTrackingSet<TagEntity>();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            getAllTagsQuery = getAllTagsQuery
                .Where(t => t.Tag.Contains(filter));
        }
        
        return getAllTagsQuery.ToListAsync();
    }
}