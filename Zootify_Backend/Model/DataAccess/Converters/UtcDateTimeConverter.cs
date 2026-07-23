using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Zootify_Backend.Model.DataAccess.Converters;

/// <summary>
/// Keeps <see cref="DateTime"/> values UTC across the SQL Server round trip.
/// The <c>DATETIME2</c> columns carry no offset, so EF materializes values with
/// <see cref="DateTimeKind.Unspecified"/>; System.Text.Json would then emit them without the
/// trailing <c>Z</c> and clients would read them as local time.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => ToUtc(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }

    /// <summary>
    /// Normalizes a value to UTC before it is written. <see cref="DateTimeKind.Unspecified"/> is
    /// treated as already-UTC rather than converted, so client-supplied dates are not shifted by
    /// the server's local offset.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The value expressed in UTC.</returns>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        _                  => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

/// <summary>
/// Nullable counterpart of <see cref="UtcDateTimeConverter"/>.
/// </summary>
public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => ToUtc(v),
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }

    /// <inheritdoc cref="UtcDateTimeConverter"/>
    private static DateTime? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return value;
        }

        return value.Value.Kind == DateTimeKind.Local
            ? value.Value.ToUniversalTime()
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }
}
