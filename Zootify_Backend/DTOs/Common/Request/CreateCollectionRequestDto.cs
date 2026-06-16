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

    /// <summary>Whether the collection is private (visible only to its owner).</summary>
    public required bool IsPrivate { get; init; }
}
