using System.Globalization;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Providers.Nominatim;

/// <summary>
/// <see cref="INominatimProvider"/> implementation that calls the public OpenStreetMap Nominatim
/// reverse-geocoding endpoint.
/// </summary>
public class NominatimProvider : INominatimProvider
{
    /// <summary>Factory used to create the HTTP client for Nominatim requests.</summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes the provider with the HTTP client factory.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create HTTP clients.</param>
    public NominatimProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Reverse-geocodes the given coordinates via Nominatim (English results).
    /// </summary>
    /// <param name="getoData">The latitude/longitude to look up.</param>
    /// <returns>The parsed Nominatim response, or <c>null</c> when the body is empty.</returns>
    /// <exception cref="ZootifyException">Thrown when the Nominatim request fails.</exception>
    public async Task<NominatimResponse?> SearchLocationAsync(GeoDto getoData)
    {
        var client = _httpClientFactory.CreateClient();

        // Nominatim's usage policy requires a descriptive User-Agent; requests without one are blocked.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Zootify-Server/1.0");

        var url =
            $"https://nominatim.openstreetmap.org/reverse" +
            $"?lat={getoData.Latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&lon={getoData.Longitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&format=json" +
            $"&accept-language=en";
        
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }
        
        return await response.Content.ReadFromJsonAsync<NominatimResponse>();
    }
}

