using System.Reflection;

namespace Zootify_Backend;

/// <summary>
/// Minimal loader for a <c>.env</c> file. On startup it searches upward from the application's base
/// directory for a file named <c>.env</c> and applies its <c>KEY=VALUE</c> entries as process environment
/// variables (without overriding variables that are already set). This lets the app read its configuration
/// from a single <c>.env</c> file both locally (<c>dotnet run</c>) and in Docker.
/// </summary>
public static class DotEnv
{
    /// <summary>
    /// Searches upward from <see cref="AppContext.BaseDirectory"/> for a <c>.env</c> file and loads it.
    /// Does nothing if no file is found.
    /// </summary>
    public static void Load()
    {
        // Only load when this assembly is the process entry point (i.e. the app is run directly via
        // `dotnet run` or `dotnet Zootify_Backend.dll`). Under the test host (WebApplicationFactory boots
        // Program too), the entry assembly is the test runner – then we skip, so a developer's local .env
        // can never influence integration-test runs. The tests provide their own configuration.
        if (Assembly.GetEntryAssembly() != Assembly.GetExecutingAssembly())
        {
            return;
        }

        var path = FindEnvFile(AppContext.BaseDirectory);

        if (path == null)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            // Skip blank lines and comments.
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            // Allow an optional "export " prefix (as in a shell script).
            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var separator = line.IndexOf('=');

            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            // Strip a single pair of surrounding single or double quotes.
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            // Never override a variable that is already set (e.g. provided by Docker or the shell).
            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    /// <summary>
    /// Walks up the directory tree from <paramref name="startDirectory"/> looking for a <c>.env</c> file.
    /// </summary>
    /// <param name="startDirectory">The directory to start the upward search from.</param>
    /// <returns>The full path to the <c>.env</c> file, or <c>null</c> if none was found.</returns>
    private static string? FindEnvFile(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
