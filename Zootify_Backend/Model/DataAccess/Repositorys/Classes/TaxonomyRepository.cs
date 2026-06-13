using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="TaxonomyNodeEntity"/> records.
/// </summary>
public class TaxonomyRepository : BaseRepository<TaxonomyNodeEntity>, ITaxonomyRepository
{
    /// <summary>
    /// Initializes a new <see cref="TaxonomyRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public TaxonomyRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the taxonomy node with the given GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the taxonomy node to retrieve.</param>
    public async Task<TaxonomyNodeEntity?> GetTaxonomyNodeByGuidAsync(Guid guid)
    {
        return await Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .Where(t => t.Guid == guid)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns all taxonomy nodes of the given rank whose name starts with the given prefix, or all nodes of that rank if no prefix is provided.
    /// </summary>
    /// <param name="startsWithName">Optional prefix to filter node names.</param>
    /// <param name="rank">The taxonomy rank to filter by.</param>
    /// <returns>The matching <see cref="TaxonomyNodeEntity"/> records.</returns>
    public async Task<List<TaxonomyNodeEntity>> GetTaxonomyNodeByStartsWithNameAsync(
        string? startsWithName,
        TaxonomyRank rank)
    {
        var query = Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .Where(tn => tn.Rank == rank);

        if (!string.IsNullOrWhiteSpace(startsWithName))
        {
            query = query
                .Where(tn => tn.Name.StartsWith(startsWithName));
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Returns all taxonomy nodes ordered by rank then name, for use in tree construction.
    /// </summary>
    /// <returns>All <see cref="TaxonomyNodeEntity"/> records ordered by rank and name.</returns>
    public async Task<List<TaxonomyNodeEntity>> GetAllNodesForTreeAsync()
    {
        return await Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .OrderBy(t => t.Rank)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }
}