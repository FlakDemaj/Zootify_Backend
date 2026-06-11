using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class UserSettingsEntity : BaseGuidEntity
{
    public Guid UserGuid { get; set; }

    public bool DarkMode { get; set; }

    public string Language { get; set; } = "de";

    public bool CheckChanges(bool darkMode, string language) =>
        DarkMode != darkMode || Language != language;

    public void Apply(bool darkMode, string language)
    {
        DarkMode = darkMode;
        Language = language;
    }
}
