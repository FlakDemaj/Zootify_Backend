namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines operations for purging soft-deleted user accounts that have exceeded the retention period.
/// </summary>
public interface IUserPurgeService
{
    /// <summary>
    /// Hard-deletes a batch of soft-deleted user accounts that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of users deleted in this batch.</returns>
    Task<int> PurgeBatchAsync();
}
