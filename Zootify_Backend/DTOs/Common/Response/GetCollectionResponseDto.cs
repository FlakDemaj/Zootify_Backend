namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A summary view of a single collection.
/// </summary>
public class GetCollectionResponseDto
{
    /// <summary>The collection's GUID.</summary>
    public required Guid Guid { get; init; }

    /// <summary>The collection's display name.</summary>
    public required string Name { get; init; }

    /// <summary>Whether the collection is public (visible to everyone, not just its owner).</summary>
    public required bool IsPublic { get; init; }

    /// <summary>The number of specimens in the collection.</summary>
    public required int SpecimenCount { get; init; }

    /// <summary>When the collection was created.</summary>
    public DateTime? CreatedAt { get; init; }
}
