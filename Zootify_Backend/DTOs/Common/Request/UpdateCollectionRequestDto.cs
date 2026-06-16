namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for updating an existing collection.
/// </summary>
public class UpdateCollectionRequestDto
{
    /// <summary>The collection's new display name.</summary>
    public required string Name { get; init; }

    /// <summary>The collection's new description.</summary>
    public string? Description { get; init; }

    /// <summary>Whether the collection is private (visible only to its owner).</summary>
    public required bool IsPrivate { get; init; }
}
