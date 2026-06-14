namespace Zootify.Tests.Unit.Domain;

public class UserSettingsEntityTests
{
    #region CheckChanges

    [Fact]
    public void CheckChanges_DarkModeChanged_ReturnsTrue()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = false,
            Language = "de",
            SendEmail = true,
            SendPushNotification = false
        };
        
        settings.CheckChanges(
            darkMode: true, 
            language: "de",
            sendPushNotification: false,
            sendEmail: true)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CheckChanges_LanguageChanged_ReturnsTrue()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = false,
            Language = "de",
            SendEmail = true,
            SendPushNotification = false
        };
        
        settings.CheckChanges(
            darkMode: false,
            language: "en",
            sendPushNotification: false,
            sendEmail: true)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CheckChanges_AllChanged_ReturnsTrue()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = false,
            Language = "de",
            SendEmail = true,
            SendPushNotification = false
        };
        
        settings.CheckChanges(
                darkMode: true,
                language: "en",
                sendPushNotification: true,
                sendEmail: false)
            .Should()
            .BeTrue();
    }
    
    [Fact]
    public void CheckChanges_ChangeNotification_ReturnsFalse()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = false,
            Language = "de",
            SendEmail = true,
            SendPushNotification = false
        };
        
        settings.CheckChanges(
                darkMode: false,
                language: "de",
                sendPushNotification: true,
                sendEmail: false)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CheckChanges_NothingChanged_ReturnsFalse()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = false,
            Language = "de",
            SendEmail = true,
            SendPushNotification = false
        };
        
        settings.CheckChanges(
                darkMode: false,
                language: "de",
                sendPushNotification: false,
                sendEmail: true)
            .Should()
            .BeFalse();
    }

    #endregion

    #region Apply

    [Fact]
    public void Apply_UpdatesDarkMode()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = false,
            Language = "de",
            SendEmail = true,
            SendPushNotification = false
        };

        settings.Apply(darkMode: true, language: "de", sendPushNotification: false, sendEmail: true);

        settings.DarkMode.Should().BeTrue();
    }

    [Fact]
    public void Apply_UpdatesLanguage()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = false,
            Language = "de",
            SendEmail = true,
            SendPushNotification = false
        };
        
        settings.Apply(darkMode: false, language: "en", sendPushNotification: false, sendEmail: true);

        settings.Language.Should().Be("en");
    }

    [Fact]
    public void Apply_UpdatesAllValues()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = true,
            Language = "de",
            SendEmail = false,
            SendPushNotification = true
        };
        
        settings.Apply(darkMode: false, language: "en", sendPushNotification: false, sendEmail: true);

        settings.DarkMode.Should().BeFalse();
        settings.Language.Should().Be("en");
        settings.SendEmail.Should().BeTrue();
        settings.SendPushNotification.Should().BeFalse();
    }
    
    [Fact]
    public void Apply_UpdatesNotification()
    {
        var settings = new UserSettingsEntity
        {
            UserGuid = Guid.NewGuid(),
            DarkMode = true,
            Language = "de",
            SendEmail = false,
            SendPushNotification = true
        };

        settings.Apply(darkMode: true, language: "de", sendPushNotification: false, sendEmail: true);

        settings.SendPushNotification.Should().BeFalse();
        settings.SendEmail.Should().BeTrue();

    }

    #endregion
}
