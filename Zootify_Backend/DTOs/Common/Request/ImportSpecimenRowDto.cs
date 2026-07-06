namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// A single parsed row from a specimen import workbook (also reused as an export row).
/// </summary>
public class ImportSpecimenRowDto
{
    /// <summary>The 1-based row number in the source workbook (used for error reporting).</summary>
    public int RowNumber { get; init; }

    /// <summary>The specimen's common display name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional latin name; falls back to the matched species' name when empty.</summary>
    public string? LatinName { get; init; }

    /// <summary>The latitude of the discovery location.</summary>
    public required double Latitude { get; init; }

    /// <summary>The longitude of the discovery location.</summary>
    public required double Longitude { get; init; }

    /// <summary>The date the specimen was discovered.</summary>
    public required DateTime DateOfDiscovery { get; init; }

    /// <summary>The latin name of the species, used to fuzzy-match the taxonomy node.</summary>
    public required string TaxonomySpeciesLatinName { get; init; }

    /// <summary>Optional free-text comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Optional size of the specimen.</summary>
    public double? Size { get; init; }

    /// <summary>Optional weight of the specimen.</summary>
    public double? Weight { get; init; }

    /// <summary>Optional storage building identifier.</summary>
    public string? StorageBuilding { get; init; }

    /// <summary>Optional storage cabinet identifier.</summary>
    public string? StorageCabinet { get; init; }

    /// <summary>Optional storage shelf identifier.</summary>
    public string? StorageShelf { get; init; }

    /// <summary>Optional storage compartment identifier.</summary>
    public string? StorageCompartment { get; init; }

    /// <summary>Comma-separated tag names (e.g. "Big, Blue"); existing tags are linked, new ones created.</summary>
    public string? Tags { get; init; }
}
