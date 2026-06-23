namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for creating a new collection.
/// </summary>
public class CreateCollectionRequestDto
{
    /// <summary>The collection's display name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description of the collection.</summary>
    public string? Description { get; init; }

    /// <summary>Whether the collection is public (visible to everyone, not just its owner).</summary>
    public required bool IsPublic { get; init; }
}
