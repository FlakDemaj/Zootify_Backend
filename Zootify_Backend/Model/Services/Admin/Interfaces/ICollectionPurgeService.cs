namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines operations for purging soft-deleted collections that have exceeded the retention period.
/// </summary>
public interface ICollectionPurgeService
{
    /// <summary>
    /// Hard-deletes a batch of soft-deleted collections that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of collections deleted in this batch.</returns>
    Task<int> PurgeBatchAsync();
}
