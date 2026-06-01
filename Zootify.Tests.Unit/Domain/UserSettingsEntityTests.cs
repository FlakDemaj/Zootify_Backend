namespace Zootify.Tests.Unit.Domain;

public class UserSettingsEntityTests
{
    #region CheckChanges

    [Fact]
    public void CheckChanges_DarkModeChanged_ReturnsTrue()
    {
        var settings = new UserSettingsEntity { UserGuid = Guid.NewGuid(), DarkMode = false, Language = "de" };

        settings.CheckChanges(darkMode: true, language: "de").Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_LanguageChanged_ReturnsTrue()
    {
        var settings = new UserSettingsEntity { UserGuid = Guid.NewGuid(), DarkMode = false, Language = "de" };

        settings.CheckChanges(darkMode: false, language: "en").Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_BothChanged_ReturnsTrue()
    {
        var settings = new UserSettingsEntity { UserGuid = Guid.NewGuid(), DarkMode = false, Language = "de" };

        settings.CheckChanges(darkMode: true, language: "en").Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_NothingChanged_ReturnsFalse()
    {
        var settings = new UserSettingsEntity { UserGuid = Guid.NewGuid(), DarkMode = true, Language = "en" };

        settings.CheckChanges(darkMode: true, language: "en").Should().BeFalse();
    }

    #endregion

    #region Apply

    [Fact]
    public void Apply_UpdatesDarkMode()
    {
        var settings = new UserSettingsEntity { UserGuid = Guid.NewGuid(), DarkMode = false, Language = "de" };

        settings.Apply(darkMode: true, language: "de");

        settings.DarkMode.Should().BeTrue();
    }

    [Fact]
    public void Apply_UpdatesLanguage()
    {
        var settings = new UserSettingsEntity { UserGuid = Guid.NewGuid(), DarkMode = false, Language = "de" };

        settings.Apply(darkMode: false, language: "en");

        settings.Language.Should().Be("en");
    }

    [Fact]
    public void Apply_UpdatesBothValues()
    {
        var settings = new UserSettingsEntity { UserGuid = Guid.NewGuid(), DarkMode = false, Language = "de" };

        settings.Apply(darkMode: true, language: "en");

        settings.DarkMode.Should().BeTrue();
        settings.Language.Should().Be("en");
    }

    #endregion
}
