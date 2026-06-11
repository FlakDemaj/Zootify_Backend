namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateSpecimenTagsRequestDto
{
    public List<UpdateTagsDto> SpecimenTags { get; init; } = [];

}