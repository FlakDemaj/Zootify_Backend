using Newtonsoft.Json;

namespace Zootify_Backend.Configuration;

/// <summary>
/// Base class for strongly-typed configuration accessors. Holds the shared <see cref="IConfiguration"/>
/// instance and provides safe (defaulting, warning-on-miss) readers for string and parsed value types.
/// </summary>
public abstract class BaseConfiguration
{
    /// <summary>The ASP.NET Core configuration instance set during initialization.</summary>
    private static IConfiguration? _configuration;

    /// <summary>Logger used for missing-key warnings and parse errors.</summary>
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

    /// <summary>
    /// Reads and parses a nullable value type, returning <paramref name="default"/> when missing or unparseable.
    /// </summary>
    /// <typeparam name="T">The target value type.</typeparam>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="default">Fallback value used when the key is missing or parsing fails.</param>
    /// <param name="warn">Whether to log a warning when the key is missing.</param>
    /// <returns>The parsed value, or the default.</returns>
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

    /// <summary>
    /// Logs a warning that configuration was accessed before initialization, noting the default used.
    /// </summary>
    /// <param name="default">The default value being returned instead.</param>
    private static void LogConfigurationNotInitialized(object @default)
    {
        _logger?.LogWarning(
            "BaseConfiguration has not been initialized. Using default value {@Default}",
            JsonConvert.SerializeObject(@default));
    }

    /// <summary>
    /// Logs a warning that a configuration key was not found, noting the default used.
    /// </summary>
    /// <param name="key">The missing configuration key.</param>
    /// <param name="default">The default value being returned instead.</param>
    private static void LogNotFound(string key, object @default)
    {
        _logger?.LogWarning(
            "Cannot find value for {Key} in configuration. Using default value {@Default}",
            key,
            JsonConvert.SerializeObject(@default));
    }

    #endregion

    #region TryParse

    /// <summary>
    /// Attempts to parse the raw string into the supported value type <typeparamref name="T"/>
    /// (double, float, int, long, or bool).
    /// </summary>
    /// <typeparam name="T">The target value type.</typeparam>
    /// <param name="value">The raw string to parse.</param>
    /// <param name="result">The parsed value when successful; otherwise the type default.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
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