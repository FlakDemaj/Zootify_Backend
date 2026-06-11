using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class TaxonomyRepository : BaseRepository<TaxonomyNodeEntity>, ITaxonomyRepository
{
    public TaxonomyRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<TaxonomyNodeEntity?> GetTaxonomyNodeByGuidAsync(Guid guid)
    {
        return await Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .Where(t => t.Guid == guid)
            .FirstOrDefaultAsync();
    }

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

    public async Task<List<TaxonomyNodeEntity>> GetAllNodesForTreeAsync()
    {
        return await Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .OrderBy(t => t.Rank)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }
}