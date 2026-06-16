namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for replacing the set of tags assigned to a specimen.
/// </summary>
public class UpdateSpecimenTagsRequestDto
{
    /// <summary>The tags the specimen should have after the update (existing and/or new).</summary>
    public List<UpdateTagsDto> SpecimenTags { get; init; } = [];
}
