using System.Text.Json.Serialization;

namespace Zootify_Backend.Model.Providers.Nominatim;

public class NominatimResponse
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }

    [JsonPropertyName("lat")]
    public string Lat { get; set; }

    [JsonPropertyName("lon")]
    public string Lon { get; set; }

    [JsonPropertyName("address")]
    public NominatimAddressResponse Address { get; set; }
}