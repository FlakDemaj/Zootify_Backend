namespace Zootify_Backend.Enums;

/// <summary>
/// The granularity of a resolved geographic location, ordered from coarsest (country) to finest (house number).
/// </summary>
public enum GeographicLevel
{
    /// <summary>Country level (coarsest).</summary>
    Country = 0,

    /// <summary>State or region level.</summary>
    State = 1,

    /// <summary>Postal-code level.</summary>
    Postcode = 2,

    /// <summary>City or town level.</summary>
    City = 3,

    /// <summary>Street level.</summary>
    Street = 4,

    /// <summary>House-number level (finest).</summary>
    HouseNumber = 5,
}
