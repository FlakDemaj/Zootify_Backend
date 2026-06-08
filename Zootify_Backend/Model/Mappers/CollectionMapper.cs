using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

public class CollectionMapper : Profile
{
    public CollectionMapper()
    {
        CreateMap<UpdateCollectionRequestDto, CollectionEntity>(MemberList.None)
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
            .ForMember(d => d.IsPrivate, o => o.MapFrom(s => s.IsPrivate));

        CreateMap<CreateCollectionRequestDto, CollectionEntity>(MemberList.None)
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
            .ForMember(d => d.IsPrivate, o => o.MapFrom(s => s.IsPrivate));

        CreateMap<CollectionEntity, GetCollectionResponseDto>()
            .ForMember(d => d.Guid, o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.IsPrivate, o => o.MapFrom(s => s.IsPrivate))
            .ForMember(d => d.SpecimenCount, o => o.MapFrom(s => s.Collection.Count));
    }
}
