namespace Zootify_Backend.Models;

// Repräsentiert einen einzelnen Fund (ein konkretes Tier/Objekt in der Sammlung)
public class Specimen
{
    public int Id { get; set; }

    // Welche Tierart ist das?
    public int SpeciesId { get; set; }
    public TaxonomyNode Species { get; set; } = null!;

    // Wo wurde es gefunden? (verknüpft mit der Ortshierarchie)
    public int? GeographicLocationId { get; set; }
    public GeographicLocation? GeographicLocation { get; set; }

    // Genaue GPS-Koordinaten des Fundorts
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? FindDate { get; set; }

    // Wem gehört das Exemplar?
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Lagerort im Museum
    public string? StorageBuilding { get; set; }
    public string? StorageCabinet { get; set; }
    public string? StorageShelf { get; set; }
    public string? StorageCompartment { get; set; }

    public string? Comment { get; set; }
    public bool IsPublic { get; set; } // Für alle sichtbar?

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SpecimenImage> Images { get; set; } = new List<SpecimenImage>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
