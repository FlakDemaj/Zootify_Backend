using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Request.Helpers;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
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
                    opt.MapFrom(src => src.DateOfDiscovery))
            .ForMember(d => d.TaxonomySpeciesGuid,
                opt =>
                    opt.MapFrom(src => src.TaxonomySpeciesGuid))
            .ForMember(d => d.StorageBuilding,
                opt =>
                    opt.MapFrom(src => src.StorageBuilding))
            .ForMember(d => d.StorageCabinet,
                opt =>
                    opt.MapFrom(src => src.StorageCabinet))
            .ForMember(d => d.StorageCompartment,
                opt => 
                    opt.MapFrom(src => src.StorageCompartment))
            .ForMember(d => d.StorageShelf,
                opt => 
                    opt.MapFrom(src => src.StorageShelf));

        CreateMap<string, TagEntity>(MemberList.None)
            .ForMember(d => d.Tag,
                opt =>
                    opt.MapFrom(src => src));

        CreateMap<SpecimenEntity, SpecimenDto>()
            .ForMember(d => d.SpecimenGuid,  o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.Name,          o => o.MapFrom(s => s.Name))
            .ForMember(d => d.LatinName,     o => o.MapFrom(s => s.LatinName))
            .ForMember(d => d.Latitude,      o => o.MapFrom(s => s.Latitude))
            .ForMember(d => d.Longitude,     o => o.MapFrom(s => s.Longitude))
            .ForMember(d => d.IsPublic,      o => o.MapFrom(s => s.IsPublic))
            .ForMember(d => d.Status,        o => o.MapFrom(s => s.Status));

        CreateMap<UpdateSpecimenRequestDto, SpecimenEntity>(MemberList.None)
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
                    opt.MapFrom(src => src.DateOfDiscovery))
            .ForMember(d => d.IsPublic,
                opt =>
                    opt.MapFrom(src => src.IsPublic))
            .ForMember(d => d.Status,
                opt =>
                    opt.MapFrom(src => src.Status));
    }
}