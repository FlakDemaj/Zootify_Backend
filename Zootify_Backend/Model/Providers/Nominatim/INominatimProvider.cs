using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;

namespace Zootify_Backend.Model.Providers.Nominatim;

public interface INominatimProvider
{
    Task<NominatimResponse?> SearchLocationAsync(GeoDto getoData);
}