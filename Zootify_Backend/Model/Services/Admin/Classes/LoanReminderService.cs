using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

/// <summary>
/// Sends staged due-date reminder emails to loan borrowers and lenders.
/// </summary>
public class LoanReminderService : ILoanReminderService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly INotificationProvider _notificationProvider;

    /// <summary>
    /// Initializes a new <see cref="LoanReminderService"/> with its required dependencies.
    /// </summary>
    /// <param name="loanRepository">Repository for loan persistence operations.</param>
    /// <param name="brevoProvider">Email provider for sending reminder emails.</param>
    /// <param name="notificationProvider">Provider for sending in-app push notifications.</param>
    public LoanReminderService(
        ILoanRepository loanRepository,
        IBrevoProvider brevoProvider,
        INotificationProvider notificationProvider)
    {
        _loanRepository = loanRepository;
        _brevoProvider = brevoProvider;
        _notificationProvider = notificationProvider;
    }

    /// <summary>
    /// Sends reminder emails for all three reminder stages and marks processed loans accordingly.
    /// </summary>
    /// <returns>The total number of loans for which reminders were sent across all stages.</returns>
    [Transaction(ReadOnly = false)]
    public async Task<int> SendReminderBatchAsync()
    {
        var today    = DateTime.UtcNow.Date;
        var batchSize = ZootifyConfiguration.LoanReminderJobBatchSize;

        // Each stage uses (threshold + 1) as exclusive upper bound so the full due-date day is included.
        var stage1Cutoff = today.AddDays(ZootifyConfiguration.LoanReminderJobStage1Days + 1);
        var stage2Cutoff = today.AddDays(ZootifyConfiguration.LoanReminderJobStage2Days + 1);
        var stage3Cutoff = today.AddDays(ZootifyConfiguration.LoanReminderJobStage3Days + 1);

        // The stages are mutually exclusive due-date bands so a single loan can only ever match one
        // stage per run (otherwise a freshly created short-term loan would receive several reminders
        // at once): stage 1 = [stage2Cutoff, stage1Cutoff), stage 2 = [stage3Cutoff, stage2Cutoff),
        // stage 3 = everything due before stage3Cutoff.
        var stage1 = await _loanRepository.GetLoansForStage1ReminderAsync(stage2Cutoff, stage1Cutoff, batchSize);
        var stage2 = await _loanRepository.GetLoansForStage2ReminderAsync(stage3Cutoff, stage2Cutoff, batchSize);
        var stage3 = await _loanRepository.GetLoansForStage3ReminderAsync(stage3Cutoff, batchSize);

        await ProcessStageAsync(stage1, stageNumber: 1, today);
        await ProcessStageAsync(stage2, stageNumber: 2, today);
        await ProcessStageAsync(stage3, stageNumber: 3, today);

        return stage1.Count + stage2.Count + stage3.Count;
    }

    /// <summary>
    /// Sends reminder emails for all loans in the given stage and records the reminder timestamp on each loan.
    /// </summary>
    /// <param name="loans">The loans to process for this reminder stage.</param>
    /// <param name="stageNumber">The reminder stage number (1, 2, or 3) determining which timestamp field to set.</param>
    /// <param name="today">The current UTC date used to calculate days until due.</param>
    private async Task ProcessStageAsync(List<LoanEntity> loans, int stageNumber, DateTime today)
    {
        if (loans.Count == 0)
        {
            return;
        }

        // All recipients across all loans are notified concurrently (each send isolated); borrower and
        // lender may prefer different languages and channels, so each is handled per their own settings.
        await ParallelNotifications.RunAsync(loans, async loan =>
        {
            var daysUntilDue = (loan.DueDate.Date - today).Days;
            var borrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim();
            var lenderName   = $"{loan.Lender.FirstName} {loan.Lender.LastName}".Trim();

            await NotifyReminderRecipientAsync(loan, loan.Borrower, borrowerName, daysUntilDue, isBorrower: true);
            await NotifyReminderRecipientAsync(loan, loan.Lender, lenderName, daysUntilDue, isBorrower: false);
        });

        foreach (var loan in loans)
        {
            switch (stageNumber)
            {
                case 1: loan.Stage1ReminderSentAt = DateTime.UtcNow; break;
                case 2: loan.Stage2ReminderSentAt = DateTime.UtcNow; break;
                case 3: loan.Stage3ReminderSentAt = DateTime.UtcNow; break;
            }
        }

        await _loanRepository.MassUpdateAsync(loans);
    }

    /// <summary>
    /// Notifies a single reminder recipient (borrower or lender) by email and/or in-app push,
    /// honoring their notification settings and preferred language.
    /// </summary>
    /// <param name="loan">The loan the reminder relates to.</param>
    /// <param name="recipient">The user to notify.</param>
    /// <param name="recipientName">The recipient's formatted full name.</param>
    /// <param name="daysUntilDue">The number of days until the loan is due (negative if overdue).</param>
    /// <param name="isBorrower">Whether the recipient is the borrower (<c>true</c>) or the lender (<c>false</c>).</param>
    private async Task NotifyReminderRecipientAsync(
        LoanEntity loan,
        UserEntity recipient,
        string recipientName,
        int daysUntilDue,
        bool isBorrower)
    {
        if (recipient.Settings.SendEmail)
        {
            await _brevoProvider.SendLoanDueReminderEmail(
                recipient.Email, recipientName,
                loan.Specimen.Name, loan.Specimen.LatinName,
                daysUntilDue, loan.DueDate, isBorrower,
                recipient.Settings.Language);
        }

        if (recipient.Settings.SendPushNotification)
        {
            await _notificationProvider.SendNotificationAsync(
                recipient,
                $"Erinnerung: Die Ausleihe für \"{loan.Specimen.Name}\" wird bald fällig.",
                $"Reminder: The loan for \"{loan.Specimen.Name}\" is due soon.");
        }
    }
}
