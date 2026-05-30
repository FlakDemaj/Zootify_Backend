using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

public class SpecimenMapper : Profile
{
    public SpecimenMapper()
    {
        CreateMap<CreateSpecimenRequestDto, SpecimenEntity>(MemberList.None)
            .ForMember(d => d.Comment,
                opt =>
                    opt.MapFrom(src => src.Comment))
            .ForMember(d => d.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(d => d.LatinName,
                opt =>
                    opt.MapFrom(src => src.LatinName))
            .ForMember(d => d.DateOfDiscovery,
                opt =>
                    opt.MapFrom(src => src.DateOfDiscovery));

        CreateMap<string, TagEntity>(MemberList.None)
            .ForMember(d => d.Tag,
                opt =>
                    opt.MapFrom(src => src));
    }
}