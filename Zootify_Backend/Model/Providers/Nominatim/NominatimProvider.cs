using System.Globalization;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Providers.Nominatim;

public class NominatimProvider : INominatimProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public NominatimProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<NominatimResponse?> SearchLocationAsync(GeoDto getoData)
    {
        var client = _httpClientFactory.CreateClient();

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

