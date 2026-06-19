using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A collection with its audit metadata and a paged list of its specimens.
/// </summary>
public class GetAllSpecimenInCollectionResponseDto
{
    /// <summary>The collection's GUID.</summary>
    public Guid CollectionGuid { get; init; }

    /// <summary>The collection's display name.</summary>
    public required string CollectionName { get; init; }
    
    /// <summary>The Guid of the user who created the collection.</summary>
    public Guid? CreatedUserGuid { get; init; }

    /// <summary>The display name of the user who created the collection.</summary>
    public string? CreatedByName { get; init; }

    /// <summary>When the collection was created.</summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>The display name of the user who last updated the collection.</summary>
    public string? UpdatedByName { get; init; }

    /// <summary>When the collection was last updated.</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>The display name of the user who deleted the collection.</summary>
    public string? DeletedByName { get; init; }

    /// <summary>When the collection was deleted.</summary>
    public DateTime? DeletedAt { get; init; }

    /// <summary>The collection's lifecycle status.</summary>
    public CollectionStatus Status { get; init; }

    /// <summary>The total number of specimens in the collection across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>The specimens on the current page.</summary>
    public List<GetAllSpecimensDto> Specimens { get; init; } = [];
}
