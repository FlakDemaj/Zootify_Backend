using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A single node in a resolved geographic path (e.g. a country, city, or street).
/// </summary>
public class GeoLocationNodeDto
{
    /// <summary>The name of the geographic entity at this level.</summary>
    public required string Name { get; init; }

    /// <summary>The granularity level this node represents.</summary>
    public GeographicLevel Level { get; init; }
}
