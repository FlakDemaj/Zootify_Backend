using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
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

        CreateMap<CreateTaxonomyNodeRequestDto, TaxonomyNodeEntity>(MemberList.None)
            .ForMember(dest => dest.Name,
                opt =>
                    opt.MapFrom(src => src.TaxonomyName))
            .ForMember(dest => dest.ParentGuid,
                opt =>
                    opt.MapFrom(src => src.ParentGuid));
    }
}