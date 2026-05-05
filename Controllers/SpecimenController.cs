using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Data;
using Zootify_Backend.DTOs;
using Zootify_Backend.Enums;
using Zootify_Backend.Models;
using Zootify_Backend.Services;

namespace Zootify_Backend.Controllers;

// Stellt alle HTTP-Endpunkte für Funde bereit (erreichbar unter /api/Specimen)
[ApiController]
[Route("api/[controller]")]
public class SpecimenController(ZootifyDbContext db, IGeocodingService geocoding) : ControllerBase
{
    // GET /api/Specimen — alle Funde zurückgeben
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SpecimenDto>>> GetAll()
    {
        var specimens = await db.Specimens
            .Include(s => s.Species)              // Tierart mitladen
            .Include(s => s.GeographicLocation)   // Fundort mitladen
            .ToListAsync();

        return Ok(specimens.Select(MapToDto));
    }

    // GET /api/Specimen/{id} — einzelnen Fund anhand der ID zurückgeben
    [HttpGet("{id}")]
    public async Task<ActionResult<SpecimenDto>> GetById(int id)
    {
        var specimen = await db.Specimens
            .Include(s => s.Species)
            .Include(s => s.GeographicLocation)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (specimen is null)
            return NotFound();

        return Ok(MapToDto(specimen));
    }

    // POST /api/Specimen — neuen Fund anlegen
    [HttpPost]
    public async Task<ActionResult<SpecimenDto>> Create(CreateSpecimenDto dto)
    {
        // Fund aus den eingehenden Daten erstellen
        var specimen = new Specimen
        {
            SpeciesId          = dto.SpeciesId,
            OwnerId            = dto.OwnerId,
            Latitude           = dto.Latitude,
            Longitude          = dto.Longitude,
            FindDate           = dto.FindDate,
            StorageBuilding    = dto.StorageBuilding,
            StorageCabinet     = dto.StorageCabinet,
            StorageShelf       = dto.StorageShelf,
            StorageCompartment = dto.StorageCompartment,
            Comment            = dto.Comment,
            IsPublic           = dto.IsPublic
        };

        // Falls Koordinaten angegeben wurden: Ort über Nominatim ermitteln und verknüpfen
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            var location = await geocoding.ResolveAsync(dto.Latitude.Value, dto.Longitude.Value);
            specimen.GeographicLocationId = location?.Id;
        }

        db.Specimens.Add(specimen);
        await db.SaveChangesAsync();

        // Verknüpfte Daten nachladen damit die Antwort vollständig ist
        await db.Entry(specimen).Reference(s => s.Species).LoadAsync();
        await db.Entry(specimen).Reference(s => s.GeographicLocation).LoadAsync();

        // 201 Created zurückgeben mit dem Link zum neu angelegten Fund
        return CreatedAtAction(nameof(GetById), new { id = specimen.Id }, MapToDto(specimen));
    }

    // PUT /api/Specimen/{id} — bestehenden Fund aktualisieren
    [HttpPut("{id}")]
    public async Task<ActionResult<SpecimenDto>> Update(int id, CreateSpecimenDto dto)
    {
        var specimen = await db.Specimens.FindAsync(id);
        if (specimen is null)
            return NotFound();

        // Felder überschreiben
        specimen.SpeciesId          = dto.SpeciesId;
        specimen.OwnerId            = dto.OwnerId;
        specimen.FindDate           = dto.FindDate;
        specimen.StorageBuilding    = dto.StorageBuilding;
        specimen.StorageCabinet     = dto.StorageCabinet;
        specimen.StorageShelf       = dto.StorageShelf;
        specimen.StorageCompartment = dto.StorageCompartment;
        specimen.Comment            = dto.Comment;
        specimen.IsPublic           = dto.IsPublic;
        specimen.UpdatedAt          = DateTime.UtcNow;

        // Geocoding nur neu aufrufen wenn sich die Koordinaten geändert haben
        if (dto.Latitude != specimen.Latitude || dto.Longitude != specimen.Longitude)
        {
            specimen.Latitude  = dto.Latitude;
            specimen.Longitude = dto.Longitude;

            if (dto.Latitude.HasValue && dto.Longitude.HasValue)
            {
                var location = await geocoding.ResolveAsync(dto.Latitude.Value, dto.Longitude.Value);
                specimen.GeographicLocationId = location?.Id;
            }
            else
            {
                specimen.GeographicLocationId = null;
            }
        }

        await db.SaveChangesAsync();

        await db.Entry(specimen).Reference(s => s.Species).LoadAsync();
        await db.Entry(specimen).Reference(s => s.GeographicLocation).LoadAsync();

        return Ok(MapToDto(specimen));
    }

    // DELETE /api/Specimen/{id} — Fund löschen
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var specimen = await db.Specimens.FindAsync(id);
        if (specimen is null)
            return NotFound();

        db.Specimens.Remove(specimen);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // Wandelt ein internes Specimen-Model in ein SpecimenDto für die API-Antwort um
    private static SpecimenDto MapToDto(Specimen s)
    {
        LocationDto? location = null;

        if (s.GeographicLocation is not null)
        {
            // Hierarchie hochlaufen und alle Ebenen sammeln (Stadt → Kreis → Bundesland → Land)
            var ancestors = new Dictionary<GeographicLevel, string>();
            var current = s.GeographicLocation;

            while (current is not null)
            {
                ancestors[current.Level] = current.Name;
                current = current.Parent;
            }

            location = new LocationDto
            {
                Id       = s.GeographicLocation.Id,
                City     = ancestors.GetValueOrDefault(GeographicLevel.City, s.GeographicLocation.Name),
                District = ancestors.GetValueOrDefault(GeographicLevel.District),
                State    = ancestors.GetValueOrDefault(GeographicLevel.State),
                Country  = ancestors.GetValueOrDefault(GeographicLevel.Country)
            };
        }

        return new SpecimenDto
        {
            Id                 = s.Id,
            SpeciesId          = s.SpeciesId,
            SpeciesName        = s.Species?.Name ?? string.Empty,
            OwnerId            = s.OwnerId,
            Latitude           = s.Latitude,
            Longitude          = s.Longitude,
            FindDate           = s.FindDate,
            Location           = location,
            StorageBuilding    = s.StorageBuilding,
            StorageCabinet     = s.StorageCabinet,
            StorageShelf       = s.StorageShelf,
            StorageCompartment = s.StorageCompartment,
            Comment            = s.Comment,
            IsPublic           = s.IsPublic,
            CreatedAt          = s.CreatedAt
        };
    }
}
