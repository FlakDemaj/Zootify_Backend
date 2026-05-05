namespace Zootify_Backend.DTOs;

// Ausgabe-Daten die der Client als Antwort bekommt
public class SpecimenDto
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public string SpeciesName { get; set; } = string.Empty; // Name der Tierart direkt als Text
    public int OwnerId { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? FindDate { get; set; }

    public LocationDto? Location { get; set; } // Aufgelöster Fundort (Stadt, Land, etc.)

    public string? StorageBuilding { get; set; }
    public string? StorageCabinet { get; set; }
    public string? StorageShelf { get; set; }
    public string? StorageCompartment { get; set; }

    public string? Comment { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Gibt den Fundort flach zurück statt als verschachtelte Hierarchie
public class LocationDto
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; } // Landkreis
    public string? State { get; set; }    // Bundesland
    public string? Country { get; set; }  // Land
}
