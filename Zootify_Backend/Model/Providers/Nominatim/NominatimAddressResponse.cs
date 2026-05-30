using System.Text.Json.Serialization;

namespace Zootify_Backend.Model.Providers.Nominatim;

public class NominatimAddressResponse
{
    [JsonPropertyName("road")]
    public string? Road { get; set; }

    [JsonPropertyName("house_number")]
    public string? HouseNumber { get; set; }
    
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }
}