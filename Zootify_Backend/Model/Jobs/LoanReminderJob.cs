using Zootify_Backend.Configuration;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

/// <summary>
/// Background job that periodically sends overdue-loan reminders in batches (each batch in its own DI scope).
/// </summary>
public class LoanReminderJob : BackgroundService
{
    /// <summary>Factory used to create a fresh DI scope (and DbContext) per batch.</summary>
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Logger used to report job progress and failures.</summary>
    private readonly ILogger<LoanReminderJob> _logger;

    /// <summary>
    /// Initializes the job with its scope factory and logger.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a fresh DI scope per batch.</param>
    /// <param name="logger">Logger used to report job progress and failures.</param>
    public LoanReminderJob(IServiceScopeFactory scopeFactory, ILogger<LoanReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs the job once on startup and then on each interval tick until cancellation.
    /// </summary>
    /// <param name="stoppingToken">Token signalled when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync();

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(ZootifyConfiguration.LoanReminderJobIntervalInHours));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAsync();
        }
    }

    /// <summary>
    /// Sends reminder batches until a batch reports zero sends, logging the total and isolating failures.
    /// </summary>
    private async Task RunAsync()
    {
        _logger.LogInformation("Loan reminder job started.");
        var totalSent = 0;

        try
        {
            int sent;
            do
            {
                // A fresh DI scope per batch gives each batch its own DbContext, so the
                // change tracker is discarded between batches instead of accumulating every
                // processed loan in memory across the whole run.
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ILoanReminderService>();
                sent = await service.SendReminderBatchAsync();
                totalSent += sent;
            }
            while (sent > 0);

            _logger.LogInformation(
                "Loan reminder job completed. Sent {Total} reminder(s).", totalSent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Loan reminder job failed after sending {Total} reminder(s).", totalSent);
        }
    }
}
