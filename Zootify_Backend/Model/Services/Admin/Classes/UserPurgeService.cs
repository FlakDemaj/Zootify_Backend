using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

public class UserPurgeService : IUserPurgeService
{
    private readonly IUserRepository _userRepository;

    public UserPurgeService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [Transaction(ReadOnly = false)]
    public async Task<int> PurgeBatchAsync()
    {
        var cutoff = DateTime.UtcNow.AddYears(-ZootifyConfiguration.CleanupJobsRetentionYears);
        var batch = await _userRepository.GetDeletedUsersForPurgeAsync(
            cutoff, ZootifyConfiguration.CleanupJobsBatchSize);

        if (batch.Count == 0) return 0;

        await _userRepository.MassDeleteAsync(batch);
        return batch.Count;
    }
}
