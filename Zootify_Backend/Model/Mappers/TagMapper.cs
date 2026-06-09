using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

public class TagMapper : Profile
{
    public TagMapper()
    {
        CreateMap<TagEntity, GetAllTagsByFilterDto>()
            .ForMember(dest => dest.TagGuid,
                opt =>
                    opt.MapFrom(src => src.Guid))
            .ForMember(dest => dest.TagName,
                opt =>
                    opt.MapFrom(src => src.Tag));
        
        CreateMap<UpdateTagsDto, TagEntity>(MemberList.None)
            .ForMember(dest => dest.Guid,
                opt =>
                    opt.MapFrom(src => src.TagGuid))
            .ForMember(dest => dest.Tag,
                opt =>
                    opt.MapFrom(src => src.TagName));
    }
}