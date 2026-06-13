using Zootify_Backend.Configuration;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

public class LoanReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LoanReminderJob> _logger;

    public LoanReminderJob(IServiceScopeFactory scopeFactory, ILogger<LoanReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

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
