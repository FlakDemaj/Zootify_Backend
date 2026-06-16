using System.Text.Json.Serialization;

namespace Zootify_Backend.Model.Providers.Nominatim;

/// <summary>
/// Deserialization model for a Nominatim reverse-geocoding response.
/// </summary>
public class NominatimResponse
{
    /// <summary>The full human-readable display name of the resolved location.</summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }

    /// <summary>The resolved latitude as a string.</summary>
    [JsonPropertyName("lat")]
    public string Lat { get; set; }

    /// <summary>The resolved longitude as a string.</summary>
    [JsonPropertyName("lon")]
    public string Lon { get; set; }

    /// <summary>The structured address breakdown of the location.</summary>
    [JsonPropertyName("address")]
    public NominatimAddressResponse Address { get; set; }
}
