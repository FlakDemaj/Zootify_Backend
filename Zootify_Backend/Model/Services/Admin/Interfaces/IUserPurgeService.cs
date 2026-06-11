namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface IUserPurgeService
{
    Task<int> PurgeBatchAsync();
}
