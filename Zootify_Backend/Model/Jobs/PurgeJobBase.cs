using Zootify_Backend.Configuration;

namespace Zootify_Backend.Model.Jobs;

/// <summary>
/// Base class for periodic background purge jobs. Runs an initial pass on startup and then repeats on a
/// fixed interval, deleting eligible records batch-by-batch (each batch in its own DI scope) until none remain.
/// </summary>
public abstract class PurgeJobBase : BackgroundService
{
    /// <summary>Factory used to create a fresh DI scope (and DbContext) per batch.</summary>
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Logger used to report job progress and failures.</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes the base job with its scope factory and logger.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a fresh DI scope per batch.</param>
    /// <param name="logger">Logger used to report job progress and failures.</param>
    protected PurgeJobBase(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>The job's display name, used in log messages.</summary>
    protected abstract string JobName { get; }

    /// <summary>How many hours to wait between runs; defaults to the shared cleanup interval.</summary>
    protected virtual int IntervalInHours => ZootifyConfiguration.CleanupJobsIntervalInHours;

    /// <summary>
    /// Purges a single batch of eligible records.
    /// </summary>
    /// <param name="serviceProvider">The batch's scoped service provider.</param>
    /// <returns>The number of records purged in this batch (0 when none remain).</returns>
    protected abstract Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider);

    /// <summary>
    /// Runs the job once on startup and then on each interval tick until cancellation.
    /// </summary>
    /// <param name="stoppingToken">Token signalled when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync();

        var interval = TimeSpan.FromHours(IntervalInHours);
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAsync();
        }
    }

    /// <summary>
    /// Executes purge batches until a batch reports zero deletions, logging the total and isolating failures.
    /// </summary>
    private async Task RunAsync()
    {
        _logger.LogInformation("{Job} started.", JobName);
        var totalDeleted = 0;

        try
        {
            int deleted;
            do
            {
                // A fresh DI scope per batch gives each batch its own DbContext, so the
                // change tracker is discarded between batches instead of accumulating every
                // purged entity in memory across the whole run.
                await using var scope = _scopeFactory.CreateAsyncScope();
                deleted = await ExecutePurgeBatchAsync(scope.ServiceProvider);
                totalDeleted += deleted;
            }
            while (deleted > 0);

            _logger.LogInformation("{Job} completed. Purged {Total} record(s).", JobName, totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Job} failed after purging {Total} record(s).", JobName, totalDeleted);
        }
    }
}
