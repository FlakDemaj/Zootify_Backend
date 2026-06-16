namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A specimen entry like <see cref="SpecimenDto"/>, additionally carrying the tags assigned to the specimen.
/// </summary>
public class SpecimenWithTagsDto : SpecimenDto
{
    /// <summary>The tags assigned to the specimen (each with GUID and name).</summary>
    public List<GetAllTagsByFilterDto> Tags { get; init; } = [];
}
