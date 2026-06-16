using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.Model.Providers.Nominatim;

/// <summary>
/// Provider for reverse-geocoding coordinates into address information via the OpenStreetMap Nominatim API.
/// </summary>
public interface INominatimProvider
{
    /// <summary>
    /// Reverse-geocodes the given coordinates into an address.
    /// </summary>
    /// <param name="getoData">The latitude/longitude to look up.</param>
    /// <returns>The Nominatim response, or <c>null</c> when no address could be resolved.</returns>
    Task<NominatimResponse?> SearchLocationAsync(GeoDto getoData);
}