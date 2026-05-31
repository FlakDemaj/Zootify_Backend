using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

public class TagMapper : Profile
{
    public TagMapper()
    {
        CreateMap<TagEntity, GetAllTagsByFilterDto>()
            .ForMember(src => src.TagGuid,
                opt =>
                    opt.MapFrom(dest => dest.Guid))
            .ForMember(src => src.TagName,
                opt =>
                    opt.MapFrom(dest => dest.Tag));
    }
}