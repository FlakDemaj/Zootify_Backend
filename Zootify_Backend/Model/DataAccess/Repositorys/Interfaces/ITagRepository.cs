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
    /// Returns all tags whose names contain the given fragment, or all tags if the filter is null or whitespace.
    /// </summary>
    /// <param name="filter">The optional name fragment to filter tag names by.</param>
    Task<List<TagEntity>> GetTagsByNameFilterAsync(string? filter);
}