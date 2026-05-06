using Newtonsoft.Json;

namespace Zootify_Backend.Configuration;

public abstract class BaseConfiguration
{
    private static IConfiguration? _configuration;
    private static ILogger? _logger;

    /// <summary>
    /// Must be called once at startup before any configuration values are accessed.
    /// </summary>
    /// <param name="configuration">The ASP.NET Core configuration instance.</param>
    /// <param name="logger">Logger used for missing key warnings and parse errors.</param>
    protected static void Initialize(
        IConfiguration configuration, ILogger<BaseConfiguration> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    #region SafeGets

    /// <summary>
    /// Reads a string value from configuration. Returns <paramref name="default"/> if the key is missing.
    /// </summary>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="default">Fallback value used when the key is not found.</param>
    /// <param name="warn">Whether to log a warning when the key is missing. Defaults to true.</param>
    protected static string SafeGet(string key, string @default, bool warn = true)
    {
        if (_configuration == null)
        {
            LogConfigurationNotInitialized(@default);
            return @default;
        }

        try
        {
            var result = _configuration[key];

            if (result == null && warn)
            {
                LogNotFound(key, @default);
            }

            return result ?? @default;
        }
        catch (ArgumentException exception)
        {
            _logger?.LogError(exception, "Exception while reading configuration key {Key}", key);
            return @default;
        }
    }

    /// <summary>
    /// Reads and parses a value type from configuration. Returns <paramref name="default"/> if missing or unparseable.
    /// </summary>
    /// <typeparam name="T">The target value type (e.g. int, bool, double).</typeparam>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="default">Fallback value used when the key is not found or parsing fails.</param>
    /// <param name="warn">Whether to log a warning when the key is missing. Defaults to true.</param>
    protected static T SafeGetParsed<T>(string key, T @default, bool warn = true)
        where T : struct
    {
        return SafeGetParsedInternal(key, (T?)@default, warn) ?? @default;
    }

    private static T? SafeGetParsedInternal<T>(string key, T? @default, bool warn = true)
        where T : struct
    {
        var raw = SafeGet(key, null!, false);

        if (TryParse<T>(raw, out var parsed))
        {
            return parsed;
        }

        if (warn)
        {
            LogNotFound(key, @default!);
        }

        return @default;
    }

    #endregion

    #region Logging

    private static void LogConfigurationNotInitialized(object @default)
    {
        _logger?.LogWarning(
            "BaseConfiguration has not been initialized. Using default value {@Default}",
            JsonConvert.SerializeObject(@default));
    }

    private static void LogNotFound(string key, object @default)
    {
        _logger?.LogWarning(
            "Cannot find value for {Key} in configuration. Using default value {@Default}",
            key,
            JsonConvert.SerializeObject(@default));
    }

    #endregion

    #region TryParse

    private static bool TryParse<T>(string value, out T? result)
        where T : struct
    {
        bool success = false;
        object? parsed = null;
        result = default;

        switch (default(T))
        {
            case double:
                success = double.TryParse(value, out var doubleResult);
                parsed = doubleResult;
                break;

            case float:
                success = float.TryParse(value, out var floatResult);
                parsed = floatResult;
                break;

            case int:
                success = int.TryParse(value, out var intResult);
                parsed = intResult;
                break;

            case long:
                success = long.TryParse(value, out var longResult);
                parsed = longResult;
                break;

            case bool:
                success = bool.TryParse(value, out var boolResult);
                parsed = boolResult;
                break;

            default:
                _logger?.LogError(
                    "No TryParse implementation found for type {TypeName}",
                    typeof(T).Name);
                break;
        }

        result = success ? (T)parsed! : default;
        return success;
    }

    #endregion
}