using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.Model.Providers.Nominatim;

public interface INominatimProvider
{
    Task<NominatimResponse?> SearchLocationAsync(GeoDto getoData);
}