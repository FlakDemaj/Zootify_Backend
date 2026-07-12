namespace Zootify_Backend.Configuration;

/// <summary>
/// Strongly-typed accessors for all Zootify configuration values (general settings, connection strings,
/// access-token, email, and background-job options), each backed by the safe readers in
/// <see cref="BaseConfiguration"/> with sensible defaults.
/// </summary>
public class ZootifyConfiguration : BaseConfiguration
{
    /// <summary>
    /// Initializes the configuration from the host's configuration and logger; must be called once at startup.
    /// </summary>
    /// <param name="configuration">The ASP.NET Core configuration instance.</param>
    /// <param name="logger">Logger used for missing-key warnings and parse errors.</param>
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
        SafeGetParsed("General:ResetPasswordTokenExpiryInHours", 24);
    
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

    #region RejectedSpecimenPurgeJob

    public static int RejectedSpecimenPurgeJobIntervalInHours =>
        SafeGetParsed("RejectedSpecimenPurgeJob:IntervalInHours", 12);

    public static int RejectedSpecimenPurgeJobRetentionDays =>
        SafeGetParsed("RejectedSpecimenPurgeJob:RetentionDays", 14);

    public static int RejectedSpecimenPurgeJobBatchSize =>
        SafeGetParsed("RejectedSpecimenPurgeJob:BatchSize", 100);

    #endregion

    #region RejectedTaxonomyPurgeJob

    public static int RejectedTaxonomyPurgeJobIntervalInHours =>
        SafeGetParsed("RejectedTaxonomyPurgeJob:IntervalInHours", 12);

    public static int RejectedTaxonomyPurgeJobRetentionDays =>
        SafeGetParsed("RejectedTaxonomyPurgeJob:RetentionDays", 14);

    public static int RejectedTaxonomyPurgeJobBatchSize =>
        SafeGetParsed("RejectedTaxonomyPurgeJob:BatchSize", 100);

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