using Zootify_Backend.Model.DataAccess.DBContext;

namespace Zootify.Tests.Integration.Utils.Helpers;

/// <summary>
/// Collection Test Repository — test support type.
/// </summary>
public class CollectionTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public CollectionTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Test support method: Create Collection Async.
    /// </summary>
    public async Task<CollectionEntity> CreateCollectionAsync(
        string name = "Test Collection",
        bool isPublic = true,
        string? description = null,
        Guid? createdByUserGuid = null,
        Guid? deletedByUserGuid = null)
    {
        var collection = new CollectionEntity
        {
            Name = name,
            IsPublic = isPublic,
            Description = description
        };

        if (createdByUserGuid.HasValue)
        {
            collection.SetCreatedBy(createdByUserGuid.Value);
        }

        if (deletedByUserGuid.HasValue)
        {
            collection.DeleteCollection(deletedByUserGuid.Value);
        }

        _dbContext.Add(collection);
        await _dbContext.SaveChangesAsync();
        return collection;
    }

    /// <summary>
    /// Test support method: Create Specimen Collection Async.
    /// </summary>
    public async Task CreateSpecimenCollectionAsync(Guid collectionGuid,
        Guid specimenGuid)
    {
        var link = new SpecimenCollectionEntity
        {
            CollectionGuid = collectionGuid,
            SpecimenGuid = specimenGuid
        };
        _dbContext.Add(link);
        await _dbContext.SaveChangesAsync();
    }
}
