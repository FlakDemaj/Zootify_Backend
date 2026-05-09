using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Models.DataAccess.Repositorys.Classes;

/// <summary>
/// Base repository class providing access to the database context.
/// /// </summary>
public class ZootifyRepository : IZootifyRepository
{
    protected ZootifyDbContext Context { get; }

    public ZootifyRepository(ZootifyDbContext context)
    {
        Context = context;
    }
}