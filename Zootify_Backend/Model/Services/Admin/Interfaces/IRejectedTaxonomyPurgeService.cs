namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines operations for purging rejected taxonomy nodes that have exceeded the configured retention period.
/// </summary>
public interface IRejectedTaxonomyPurgeService
{
    /// <summary>
    /// Hard-deletes a batch of rejected taxonomy nodes that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of taxonomy nodes deleted in this batch.</returns>
    Task<int> PurgeBatchAsync();
}
