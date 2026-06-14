using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Holds a user's preferences such as dark mode, notification channels, and language.
/// </summary>
public class UserSettingsEntity : BaseGuidEntity
{
    public Guid UserGuid { get; set; }

    public bool DarkMode { get; set; }
    
    public bool SendPushNotification { get; set; } = true;

    public bool SendEmail { get; set; } = true;
    
    public string Language { get; set; } = "de";
    
    /// <summary>
    /// Determines whether the given settings values differ from the current ones.
    /// </summary>
    /// <param name="darkMode">The candidate dark-mode value.</param>
    /// <param name="sendEmail">The candidate email-notification value.</param>
    /// <param name="sendPushNotification">The candidate push-notification value.</param>
    /// <param name="language">The candidate language value.</param>
    /// <returns><c>true</c> if any of the values differ; otherwise <c>false</c>.</returns>
    public bool CheckChanges(
        bool darkMode,
        bool sendEmail,
        bool sendPushNotification,
        string language) =>
        DarkMode != darkMode
        || Language != language
        || SendPushNotification != sendPushNotification
        || SendEmail != sendEmail;

    /// <summary>
    /// Applies the given settings values to this entity.
    /// </summary>
    /// <param name="sendPushNotification">Whether push notifications are enabled.</param>
    /// <param name="sendEmail">Whether email notifications are enabled.</param>
    /// <param name="darkMode">Whether dark mode is enabled.</param>
    /// <param name="language">The preferred language code (e.g. "de" or "en").</param>
    public void Apply(
        bool sendPushNotification,
        bool sendEmail,
        bool darkMode,
        string language)
    {
        DarkMode = darkMode;
        Language = language;
        SendPushNotification = sendPushNotification;
        SendEmail = sendEmail;
    }
}
