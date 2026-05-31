using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class TaxonomyRepository : BaseRepository<TaxonomyNodeEntity>, ITaxonomyRepository
{
    public TaxonomyRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<TaxonomyNodeEntity?> GetTaxonomyNodeByGuidAsync(Guid guid)
    {
        try
        {
            return await Context
                .GetTrackingSet<TaxonomyNodeEntity>()
                .Where(t => t.Guid == guid)
                .FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<TaxonomyNodeEntity>> GetTaxonomyNodeByStartsWithNameAsync(
        string? startsWithName)
    {
        var query = Context
            .GetTrackingSet<TaxonomyNodeEntity>();

        if (!string.IsNullOrWhiteSpace(startsWithName))
        {
            query = query
                .Where(tn => tn.Name.StartsWith(startsWithName));
        }
        
        return await query.ToListAsync();
    }
}