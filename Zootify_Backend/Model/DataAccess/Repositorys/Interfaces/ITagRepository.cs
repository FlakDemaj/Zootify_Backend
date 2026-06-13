using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with tag-specific query operations.
/// </summary>
public interface ITagRepository : IBaseRepository<TagEntity>
{
    /// <summary>
    /// Returns all tags whose GUIDs are in the given list.
    /// </summary>
    /// <param name="guids">The list of tag GUIDs to look up.</param>
    Task<List<TagEntity>> GetTagByGuidAsync(List<Guid> guids);

    /// <summary>
    /// Returns all tags whose names start with the given prefix, or all tags if the prefix is null or whitespace.
    /// </summary>
    /// <param name="startsWith">The optional prefix to filter tag names by.</param>
    Task<List<TagEntity>> GetTagsByStartsWithNameAsync(string? startsWith);
}