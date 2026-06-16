using System.Text.Json.Serialization;

namespace Zootify_Backend.Model.Providers.Nominatim;

/// <summary>
/// Deserialization model for the structured address part of a Nominatim response.
/// </summary>
public class NominatimAddressResponse
{
    /// <summary>The street/road name.</summary>
    [JsonPropertyName("road")]
    public string? Road { get; set; }

    /// <summary>The house number.</summary>
    [JsonPropertyName("house_number")]
    public string? HouseNumber { get; set; }

    /// <summary>The ISO country code.</summary>
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    /// <summary>The state or region.</summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>The city or town.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>The postal code.</summary>
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }
}
