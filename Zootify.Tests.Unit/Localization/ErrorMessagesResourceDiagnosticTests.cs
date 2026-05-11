using System.Globalization;
using System.Resources;
using Zootify_Backend.Resources;

namespace Zootify.Tests.Unit.Localization;

/// <summary>
/// Diagnostic tests to verify that the embedded resource names match
/// what <see cref="ResourceManager"/> expects.
/// Run these when localization returns keys instead of translated strings.
/// </summary>
public class ErrorMessagesResourceDiagnosticTests
{
    private const string ExpectedBaseName = "Zootify_Backend.Resources.ErrorMessages";

    /// <summary>
    /// Prints all embedded resource names in the assembly.
    /// If this test fails, check the output — the actual names tell you
    /// what <c>ResourcesPath</c> and the marker class namespace need to be.
    /// </summary>
    [Fact]
    public void Assembly_ContainsErrorMessagesResource()
    {
        var assembly = typeof(ErrorMessages).Assembly;
        var allNames = assembly.GetManifestResourceNames();

        allNames.Should().Contain(
            name => name.Contains("ErrorMessages"),
            $"no resource containing 'ErrorMessages' found. Actual resource names:{Environment.NewLine}" +
            string.Join(Environment.NewLine, allNames));
    }

    /// <summary>
    /// Verifies that the exact expected base name exists as an embedded resource.
    /// </summary>
    [Fact]
    public void Assembly_ContainsExpectedBaseName()
    {
        var assembly = typeof(ErrorMessages).Assembly;
        var allNames = assembly.GetManifestResourceNames();

        allNames.Should().Contain(
            $"{ExpectedBaseName}.resources",
            $"expected resource '{ExpectedBaseName}.resources' not found. Actual:{Environment.NewLine}" +
            string.Join(Environment.NewLine, allNames));
    }

    /// <summary>
    /// Verifies that the ResourceManager can open the default resource set
    /// without throwing and that it contains at least one entry.
    /// </summary>
    [Fact]
    public void ResourceManager_CanOpenDefaultResourceSet()
    {
        var assembly = typeof(ErrorMessages).Assembly;
        var manager = new ResourceManager(ExpectedBaseName, assembly);

        var set = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);

        set.Should().NotBeNull("the default resource set must be loadable");
        set!.Cast<System.Collections.DictionaryEntry>().Should().NotBeEmpty();
    }
}
