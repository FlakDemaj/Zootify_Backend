namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines operations for purging soft-deleted specimens that have exceeded the retention period.
/// </summary>
public interface ISpecimenPurgeService
{
    /// <summary>
    /// Hard-deletes a batch of soft-deleted specimens that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of specimens deleted in this batch.</returns>
    Task<int> PurgeBatchAsync();
}
