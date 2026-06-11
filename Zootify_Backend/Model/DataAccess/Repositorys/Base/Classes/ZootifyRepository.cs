using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;

/// <summary>
/// Base repository class providing access to the database context.
/// </summary>
public class ZootifyRepository : IZootifyRepository
{
    protected ZootifyDbContext Context { get; }

    /// <summary>
    /// Initializes a new <see cref="ZootifyRepository"/> with the given database context.
    /// </summary>
    public ZootifyRepository(ZootifyDbContext context)
    {
        Context = context;
    }
}
