namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines operations for purging rejected specimens that have exceeded the configured retention period.
/// </summary>
public interface IRejectedSpecimenPurgeService
{
    /// <summary>
    /// Hard-deletes a batch of rejected specimens that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of specimens deleted in this batch.</returns>
    Task<int> PurgeBatchAsync();
}
