using Zootify_Backend.DTOs.Common.Request.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllSpecimenInCollectionResponseDto
{
    public Guid CollectionGuid { get; set; }

    public required string CollectionName { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? UpdatedByName { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? DeletedByName { get; set; }

    public DateTime? DeletedAt { get; set; }

    public CollectionStatus Status { get; set; }

    public int TotalCount { get; set; }

    public List<SpecimenDto> Specimens { get; set; } = [];
}
