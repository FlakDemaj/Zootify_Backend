using Microsoft.Extensions.Logging.Abstractions;

namespace Zootify_Backend.Configuration;

public class ZootifyConfiguration : BaseConfiguration
{
    public static void InitializeFromHost(
        IConfiguration configuration,
        ILogger<BaseConfiguration> logger)
    {
        Initialize(configuration, logger);
    }

    #region General

    public static string SpecialCharacters =>
        SafeGet("General:SpecialCharacters", string.Empty);
    
    public static int MinimumPasswordLength =>
        SafeGetParsed("General:MinimumPasswordLength", 0);
    
    public static int MaximumLoginAttempts =>
        SafeGetParsed("General:MaximumLoginAttempts", 5);
    
    public static int RegistrationTokenExpiryInHours =>
        SafeGetParsed("General:RegistrationTokenExpiryInHours", 24);
    
    public static int ResetPasswordTokenExpiryInHours =>
        SafeGetParsed("General:RegistrationTokenExpiryInHours", 24);
    
    public static string FrontendBaseUrl =>
        SafeGet("General:FrontendBaseUrl", string.Empty);

    #endregion

    #region ConnectionStrings

    public static string DatabaseDefaultConnectionString =>
        SafeGet("ConnectionStrings:DefaultConnectionString", string.Empty);

    public static string DatabaseOverrideConnectionString =>
        SafeGet("ConnectionStrings:OverrideConnectionString", string.Empty);

    #endregion

    #region AccessToken

    public static int AccessTokenExpiresInMinutes =>
        SafeGetParsed("AccessToken:ExpiresInMinutes", 0);
    
    public static string AccessTokenIssuer =>
        SafeGet("AccessToken:Issuer", string.Empty);

    public static string AccessTokenAudience =>
        SafeGet("AccessToken:Audience", string.Empty);

    public static string AccessTokenKey =>
        SafeGet("AccessToken:Key", string.Empty);

    #endregion

    #region Brevo

    public static string ZootifyEmail =>
        SafeGet("Brevo:Email", string.Empty);

    public static string ZootifyName =>
        SafeGet("Brevo:SenderName", string.Empty);

    public static string BrevoApiKey =>
        SafeGet("Brevo:ApiKey", string.Empty);

    #endregion

    #region CleanupJobs

    public static int CleanupJobsIntervalInHours =>
        SafeGetParsed("CleanupJobs:IntervalInHours", 12);

    public static int CleanupJobsRetentionYears =>
        SafeGetParsed("CleanupJobs:RetentionYears", 3);

    public static int CleanupJobsBatchSize =>
        SafeGetParsed("CleanupJobs:BatchSize", 100);

    #endregion

    #region LoanReminderJob

    public static int LoanReminderJobIntervalInHours =>
        SafeGetParsed("LoanReminderJob:IntervalInHours", 12);

    public static int LoanReminderJobBatchSize =>
        SafeGetParsed("LoanReminderJob:BatchSize", 50);

    public static int LoanReminderJobStage1Days =>
        SafeGetParsed("LoanReminderJob:Stage1Days", 7);

    public static int LoanReminderJobStage2Days =>
        SafeGetParsed("LoanReminderJob:Stage2Days", 4);

    public static int LoanReminderJobStage3Days =>
        SafeGetParsed("LoanReminderJob:Stage3Days", 1);

    #endregion
}