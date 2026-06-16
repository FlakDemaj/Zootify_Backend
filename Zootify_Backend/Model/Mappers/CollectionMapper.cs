using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

/// <summary>
/// AutoMapper profile defining the mappings between collection DTOs and <see cref="CollectionEntity"/>.
/// </summary>
public class CollectionMapper : Profile
{
    /// <summary>
    /// Registers the create/update request and response mappings for collections.
    /// </summary>
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
            .ForMember(d => d.SpecimenCount, o => o.MapFrom(s => s.Collection.Count))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreationDate));

    }
}
