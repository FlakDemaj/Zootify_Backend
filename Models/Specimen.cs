namespace Zootify_Backend.Models;

public class Specimen
{
    public int Id { get; set; }

    public int SpeciesId { get; set; }
    public TaxonomyNode Species { get; set; } = null!;

    public int? GeographicLocationId { get; set; }
    public GeographicLocation? GeographicLocation { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? FindDate { get; set; }

    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string? StorageBuilding { get; set; }
    public string? StorageCabinet { get; set; }
    public string? StorageShelf { get; set; }
    public string? StorageCompartment { get; set; }

    public string? Comment { get; set; }
    public bool IsPublic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SpecimenImage> Images { get; set; } = new List<SpecimenImage>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
