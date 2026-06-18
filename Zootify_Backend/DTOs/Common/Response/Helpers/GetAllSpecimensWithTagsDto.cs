namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A specimen entry like <see cref="GetAllSpecimensDto"/>, additionally carrying the tags assigned to the specimen.
/// </summary>
public class GetAllSpecimensWithTagsDto : GetAllSpecimensDto
{
    /// <summary>The tags assigned to the specimen (each with GUID and name).</summary>
    public List<GetAllTagsByFilterDto> Tags { get; init; } = [];
}
