using Zootify_Backend.DTOs.Common.Request;

namespace Zootify_Backend.Model.Providers.Nominatim;

public interface INominatimProvider
{
    Task<NominatimResponse?> SearchLocationAsync(GeoDto getoData);
}