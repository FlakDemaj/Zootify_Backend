using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

/// <summary>
/// Purges soft-deleted user accounts that have exceeded the configured retention period.
/// </summary>
public class UserPurgeService : IUserPurgeService
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new <see cref="UserPurgeService"/> with its required repository.
    /// </summary>
    /// <param name="userRepository">Repository for user persistence operations.</param>
    public UserPurgeService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Hard-deletes a batch of soft-deleted user accounts that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of users deleted in this batch.</returns>
    [Transaction(ReadOnly = false)]
    public async Task<int> PurgeBatchAsync()
    {
        var cutoff = DateTime.UtcNow.AddYears(-ZootifyConfiguration.CleanupJobsRetentionYears);
        var batch = await _userRepository.GetDeletedUsersForPurgeAsync(
            cutoff, ZootifyConfiguration.CleanupJobsBatchSize);

        if (batch.Count == 0)
        {
            return 0;
        }

        await _userRepository.MassDeleteAsync(batch);
        return batch.Count;
    }
}
