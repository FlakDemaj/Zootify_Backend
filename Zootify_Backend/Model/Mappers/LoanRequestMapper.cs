using AutoMapper;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

/// <summary>
/// AutoMapper profile that defines mappings for loan-request-related types.
/// </summary>
public class LoanRequestMapper : Profile
{
    /// <summary>
    /// Configures the mappings from <see cref="LoanRequestEntity"/> to its overview and detail DTOs.
    /// </summary>
    public LoanRequestMapper()
    {
        CreateMap<LoanRequestEntity, GetLoanRequestOverviewDto>()
            .ForMember(d => d.RequestGuid,
                o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.SpecimenGuid,
                o => o.MapFrom(s => s.SpecimenGuid))
            .ForMember(d => d.SpecimenName,
                o => o.MapFrom(s => s.Specimen.Name))
            .ForMember(d => d.SpecimenLatinName,
                o => o.MapFrom(s => s.Specimen.LatinName))
            .ForMember(d => d.ImageGuid,
                o => o.MapFrom(s => s.Specimen.ImageGuid))
            .ForMember(d => d.RequesterGuid,
                o => o.MapFrom(s => s.RequesterGuid))
            .ForMember(d => d.RequesterName,
                o => o.MapFrom(s => $"{s.Requester.FirstName} {s.Requester.LastName}"))
            .ForMember(d => d.DesiredDueDate,
                o => o.MapFrom(s => s.DesiredDueDate))
            .ForMember(d => d.RequestedAt,
                o => o.MapFrom(s => s.RequestedAt))
            .ForMember(d => d.Status,
                o => o.MapFrom(s => s.Status))
            .ForMember(d => d.AdminApproved,
                o => o.MapFrom(s => s.AdminApprovedAt != null))
            .ForMember(d => d.CreatorApprovalRequired,
                o => o.MapFrom(s => s.Specimen.CreatedByUserGuid != null
                                    && s.Specimen.CreatedByUserGuid != s.RequesterGuid))
            .ForMember(d => d.CreatorApproved,
                o => o.MapFrom(s => s.CreatorApprovedAt != null));

        CreateMap<LoanRequestEntity, GetLoanRequestDetailDto>()
            .ForMember(d => d.RequestGuid,
                o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.SpecimenGuid,
                o => o.MapFrom(s => s.SpecimenGuid))
            .ForMember(d => d.SpecimenName,
                o => o.MapFrom(s => s.Specimen.Name))
            .ForMember(d => d.SpecimenLatinName,
                o => o.MapFrom(s => s.Specimen.LatinName))
            .ForMember(d => d.ImageGuid,
                o => o.MapFrom(s => s.Specimen.ImageGuid))
            .ForMember(d => d.RequesterGuid,
                o => o.MapFrom(s => s.RequesterGuid))
            .ForMember(d => d.RequesterName,
                o => o.MapFrom(s => $"{s.Requester.FirstName} {s.Requester.LastName}"))
            .ForMember(d => d.DesiredDueDate,
                o => o.MapFrom(s => s.DesiredDueDate))
            .ForMember(d => d.RequesterNote,
                o => o.MapFrom(s => s.RequesterNote))
            .ForMember(d => d.RequestedAt,
                o => o.MapFrom(s => s.RequestedAt))
            .ForMember(d => d.Status,
                o => o.MapFrom(s => s.Status))
            .ForMember(d => d.AdminApproved,
                o => o.MapFrom(s => s.AdminApprovedAt != null))
            .ForMember(d => d.AdminApprovedAt,
                o => o.MapFrom(s => s.AdminApprovedAt))
            .ForMember(d => d.CreatorApprovalRequired,
                o => o.MapFrom(s => s.Specimen.CreatedByUserGuid != null
                                    && s.Specimen.CreatedByUserGuid != s.RequesterGuid))
            .ForMember(d => d.CreatorApproved,
                o => o.MapFrom(s => s.CreatorApprovedAt != null))
            .ForMember(d => d.CreatorApprovedAt,
                o => o.MapFrom(s => s.CreatorApprovedAt))
            .ForMember(d => d.RejectionReason,
                o => o.MapFrom(s => s.RejectionReason))
            .ForMember(d => d.RejectedAt,
                o => o.MapFrom(s => s.RejectedAt))
            .ForMember(d => d.CreatedLoanGuid,
                o => o.MapFrom(s => s.CreatedLoanGuid));
    }
}
