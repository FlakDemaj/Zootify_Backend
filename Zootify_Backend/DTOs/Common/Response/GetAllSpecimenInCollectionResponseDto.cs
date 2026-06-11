using Zootify_Backend.DTOs.Common.Request.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllSpecimenInCollectionResponseDto
{
    public Guid CollectionGuid { get; init; }

    public required string CollectionName { get; init; }

    public string? CreatedByName { get; init; }

    public DateTime? CreatedAt { get; init; }

    public string? UpdatedByName { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public string? DeletedByName { get; init; }

    public DateTime? DeletedAt { get; init; }

    public CollectionStatus Status { get; init; }

    public int TotalCount { get; init; }

    public List<SpecimenDto> Specimens { get; init; } = [];
}
