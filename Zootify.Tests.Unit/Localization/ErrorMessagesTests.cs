using System.Collections;
using System.Globalization;
using System.Resources;

using Zootify_Backend.Enums;
using Zootify_Backend.Resources;

namespace Zootify.Tests.Unit.Localization;

/// <summary>
/// Verifies that all language-specific resource files are complete and consistent
/// with each other and with the <see cref="ErrorCode"/> enum.
/// </summary>
public class ErrorMessagesTests
{
    private const string ResourceBaseName = "Zootify_Backend.Resources.ErrorMessages";

    /// <summary>
    /// Creates a <see cref="ResourceManager"/> pointing at the ErrorMessages resource family
    /// in the main application assembly.
    /// </summary>
    private static ResourceManager CreateResourceManager() =>
        new(ResourceBaseName, typeof(ErrorMessages).Assembly);

    /// <summary>
    /// Returns all string entries from a <see cref="ResourceSet"/> as a dictionary keyed by name.
    /// </summary>
    private static Dictionary<string, string> GetEntries(ResourceSet set) =>
        set.Cast<DictionaryEntry>()
           .ToDictionary(e => (string)e.Key, e => (string)e.Value!);

    /// <summary>
    /// Verifies that the default (German) resource file and the English resource file
    /// contain the same number of entries so no translation is missing.
    /// </summary>
    [Fact]
    public void ErrorMessages_DefaultAndEnglish_HaveSameEntryCount()
    {
        var manager = CreateResourceManager();

        var defaultSet = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
        var enSet      = manager.GetResourceSet(new CultureInfo("en"),          createIfNotExists: true, tryParents: false);

        defaultSet.Should().NotBeNull("the default resource file must exist");
        enSet.Should().NotBeNull("the English resource file must exist");

        var defaultCount = GetEntries(defaultSet!).Count;
        var enCount      = GetEntries(enSet!).Count;

        defaultCount.Should().Be(enCount,
            "every entry in the default resource file must have an English translation");
    }

    /// <summary>
    /// Verifies that the default resource file has exactly one entry per <see cref="ErrorCode"/>
    /// enum value, so no error code is silently missing a message.
    /// </summary>
    [Fact]
    public void ErrorMessages_Default_HasEntryForEveryErrorCode()
    {
        var manager = CreateResourceManager();

        var defaultSet = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
        defaultSet.Should().NotBeNull("the default resource file must exist");

        var entries        = GetEntries(defaultSet!);
        var errorCodeNames = Enum.GetNames<ErrorCode>();

        entries.Count.Should().Be(errorCodeNames.Length,
            "there must be exactly one resource entry per ErrorCode value");
    }

    /// <summary>
    /// Verifies that the English resource file has exactly one entry per <see cref="ErrorCode"/>
    /// enum value, so no error code is silently missing an English message.
    /// </summary>
    [Fact]
    public void ErrorMessages_English_HasEntryForEveryErrorCode()
    {
        var manager = CreateResourceManager();

        var enSet = manager.GetResourceSet(new CultureInfo("en"), createIfNotExists: true, tryParents: false);
        enSet.Should().NotBeNull("the English resource file must exist");

        var entries        = GetEntries(enSet!);
        var errorCodeNames = Enum.GetNames<ErrorCode>();

        entries.Count.Should().Be(errorCodeNames.Length,
            "there must be exactly one English resource entry per ErrorCode value");
    }

    /// <summary>
    /// Verifies that both resource files contain entries for identical keys,
    /// so a translation is not accidentally added under a wrong name.
    /// </summary>
    [Fact]
    public void ErrorMessages_DefaultAndEnglish_HaveIdenticalKeys()
    {
        var manager = CreateResourceManager();

        var defaultSet = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
        var enSet      = manager.GetResourceSet(new CultureInfo("en"),          createIfNotExists: true, tryParents: false);

        defaultSet.Should().NotBeNull();
        enSet.Should().NotBeNull();

        var defaultKeys = GetEntries(defaultSet!).Keys.Order().ToList();
        var enKeys      = GetEntries(enSet!).Keys.Order().ToList();

        defaultKeys.Should().BeEquivalentTo(enKeys,
            "every key in the default resource file must also exist in the English resource file and vice versa");
    }
}
