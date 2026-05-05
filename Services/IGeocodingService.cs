using Zootify_Backend.Models;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Services;

// Schnittstelle für den Geocoding-Service — gibt vor welche Methoden vorhanden sein müssen
public interface IGeocodingService
{
    // Nimmt Koordinaten entgegen und gibt den passenden Ort zurück
    Task<GeographicLocationEntity?> ResolveAsync(double latitude, double longitude);
}
