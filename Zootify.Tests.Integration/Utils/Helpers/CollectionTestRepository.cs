namespace Zootify.Tests.Integration.Utils.Helpers;

public class CollectionTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public CollectionTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CollectionEntity> CreateCollectionAsync(
        string name = "Test Collection",
        bool isPrivate = false,
        string? description = null,
        Guid? createdByUserGuid = null,
        Guid? deletedByUserGuid = null)
    {
        var collection = new CollectionEntity
        {
            Name = name,
            IsPrivate = isPrivate,
            Description = description
        };

        if (createdByUserGuid.HasValue)
            collection.SetCreatedBy(createdByUserGuid.Value);

        if (deletedByUserGuid.HasValue)
            collection.SetDeletedBy(deletedByUserGuid.Value);

        _dbContext.Add(collection);
        await _dbContext.SaveChangesAsync();
        return collection;
    }

    public async Task<SpecimenCollectionEntity> CreateSpecimenCollectionAsync(
        Guid collectionGuid,
        Guid specimenGuid)
    {
        var link = new SpecimenCollectionEntity
        {
            CollectionGuid = collectionGuid,
            SpecimenGuid = specimenGuid
        };
        _dbContext.Add(link);
        await _dbContext.SaveChangesAsync();
        return link;
    }
}
