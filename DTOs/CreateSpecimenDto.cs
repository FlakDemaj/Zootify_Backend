namespace Zootify_Backend.DTOs;

// Eingabe-Daten die der Client beim Anlegen eines Funds schickt
public class CreateSpecimenDto
{
    public int SpeciesId { get; set; }  // ID der Tierart
    public int OwnerId { get; set; }    // ID des Besitzers

    public double? Latitude { get; set; }   // Breitengrad (optional)
    public double? Longitude { get; set; }  // Längengrad (optional)
    public DateTime? FindDate { get; set; } // Funddatum (optional)

    // Lagerort im Museum (alle optional)
    public string? StorageBuilding { get; set; }
    public string? StorageCabinet { get; set; }
    public string? StorageShelf { get; set; }
    public string? StorageCompartment { get; set; }

    public string? Comment { get; set; } // Freitext-Kommentar
    public bool IsPublic { get; set; }   // Sichtbar für alle?
}
