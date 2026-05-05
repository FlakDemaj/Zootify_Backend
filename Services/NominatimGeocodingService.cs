using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Data;
using Zootify_Backend.Enums;
using Zootify_Backend.Models;

namespace Zootify_Backend.Services;

// Ruft die OpenStreetMap-API (Nominatim) auf um aus Koordinaten einen Ort zu ermitteln
public class NominatimGeocodingService(HttpClient httpClient, ZootifyDbContext db) : IGeocodingService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Hauptmethode: nimmt Koordinaten, gibt den spezifischsten Ort zurück (Stadt > Kreis > Bundesland > Land)
    public async Task<GeographicLocation?> ResolveAsync(double latitude, double longitude)
    {
        var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json&addressdetails=1";

        // API aufrufen
        using var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        // JSON-Antwort einlesen und in ein Objekt umwandeln
        using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<NominatimResponse>(stream, JsonOptions);

        if (result?.Address is null)
            return null;

        var address = result.Address;

        // Hierarchie von oben nach unten aufbauen: Land → Bundesland → Kreis → Stadt
        // Jede Ebene wird nur einmal in der DB angelegt und danach wiederverwendet
        var country  = await FindOrCreateAsync(address.Country, GeographicLevel.Country,  null);
        if (country is null) return null;

        var state    = await FindOrCreateAsync(address.State,   GeographicLevel.State,    country.Id);
        var district = await FindOrCreateAsync(address.County,  GeographicLevel.District, state?.Id ?? country.Id);
        var city     = await FindOrCreateAsync(
            address.City ?? address.Town ?? address.Village,    // Nominatim nutzt je nach Ort unterschiedliche Felder
            GeographicLevel.City,
            district?.Id ?? state?.Id ?? country.Id);

        // Den spezifischsten gefundenen Ort zurückgeben
        return city ?? district ?? state ?? country;
    }

    // Sucht einen Ort in der DB — legt ihn neu an falls er noch nicht existiert
    private async Task<GeographicLocation?> FindOrCreateAsync(string? name, GeographicLevel level, int? parentId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Prüfen ob der Ort bereits in der DB vorhanden ist
        var existing = await db.GeographicLocations
            .FirstOrDefaultAsync(g => g.Name == name && g.ParentId == parentId);

        if (existing is not null)
            return existing;

        // Neuen Ort anlegen und speichern
        var location = new GeographicLocation
        {
            Name     = name,
            Level    = level,
            ParentId = parentId
        };

        db.GeographicLocations.Add(location);
        await db.SaveChangesAsync();
        return location;
    }

    // Hilfsklassen zum Deserialisieren der Nominatim-API-Antwort
    private sealed class NominatimResponse
    {
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        public string? Country { get; set; }
        public string? State   { get; set; }
        public string? County  { get; set; }
        public string? City    { get; set; }
        public string? Town    { get; set; }
        public string? Village { get; set; }
    }
}
