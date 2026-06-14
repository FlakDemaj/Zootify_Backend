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
    /// Returns all tags whose name starts with the given prefix, or all tags if no prefix is provided.
    /// </summary>
    /// <param name="startsWith">Optional prefix to filter tag names.</param>
    /// <returns>The matching <see cref="TagEntity"/> records.</returns>
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