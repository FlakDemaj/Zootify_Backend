namespace Zootify_Backend.DTOs.Common.Request;

public class ImportSpecimenRowDto
{
    public int RowNumber { get; init; }

    public required string Name { get; init; }

    public string? LatinName { get; init; }

    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required DateTime DateOfDiscovery { get; init; }

    public required string TaxonomySpeciesLatinName { get; init; }

    public string? Comment { get; init; }

    public string? StorageBuilding { get; init; }

    public string? StorageCabinet { get; init; }

    public string? StorageShelf { get; init; }

    public string? StorageCompartment { get; init; }

    /// <summary>Comma-separated tag names (e.g. "Big, Blue"); existing tags are linked, new ones created.</summary>
    public string? Tags { get; init; }
}
