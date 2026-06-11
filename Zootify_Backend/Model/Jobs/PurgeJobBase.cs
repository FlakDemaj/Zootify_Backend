using Zootify_Backend.Configuration;

namespace Zootify_Backend.Model.Jobs;

public abstract class PurgeJobBase : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    protected PurgeJobBase(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected abstract string JobName { get; }

    protected virtual int IntervalInHours => ZootifyConfiguration.CleanupJobsIntervalInHours;

    protected abstract Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider);

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

    private async Task RunAsync()
    {
        _logger.LogInformation("{Job} started.", JobName);
        var totalDeleted = 0;

        try
        {
            int deleted;
            do
            {
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
