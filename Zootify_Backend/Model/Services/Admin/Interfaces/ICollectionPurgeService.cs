namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface ICollectionPurgeService
{
    Task<int> PurgeBatchAsync();
}
