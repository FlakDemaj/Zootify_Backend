using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

public class TaxonomyMapper : Profile
{
    public TaxonomyMapper()
    {
        CreateMap<TaxonomyNodeEntity, GetAllTaxonomyNodesByFilterDto>(MemberList.None)
            .ForMember(dest => dest.TaxonomyGuid,
                opt =>
                    opt.MapFrom(src => src.Guid))
            .ForMember(dest => dest.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Rank,
                opt =>
                    opt.MapFrom(src => src.Rank))
            .ForMember(dest => dest.ApprovalStatus,
                opt =>
                    opt.MapFrom(src => src.ApprovalStatus));

        CreateMap<KeyValuePair<TaxonomyRank, string>, TaxonomyNodeEntity>(MemberList.None)
            .ForMember(dest => dest.Rank,
                opt =>
                    opt.MapFrom(src => src.Key))
            .ForMember(dest => dest.Name,
                opt =>
                    opt.MapFrom(src => src.Value));

        CreateMap<TaxonomyNodeEntity, CreatedObjectResponseDto>()
            .ForMember(dest => dest.CreatedObjectGuid,
                opt =>
                    opt.MapFrom(src => src.Guid));
        
        CreateMap<List<GetTaxonomyTreeNodeDto>, GetTaxonomyTreeResponseDto>(MemberList.None)
            .ForMember(dest => dest.Nodes, 
                opt => 
                    opt.MapFrom(src => src))
            .ForMember(dest => dest.TotalCount, 
                opt => 
                    opt.MapFrom(src => src.Count));
    }
}