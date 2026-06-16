namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A geographic coordinate pair.
/// </summary>
public class GeoDto
{
    /// <summary>The latitude in decimal degrees.</summary>
    public double Latitude { get; init; }

    /// <summary>The longitude in decimal degrees.</summary>
    public double Longitude { get; init; }
}
