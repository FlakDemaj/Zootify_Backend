using AutoMapper;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

/// <summary>
/// AutoMapper profile that defines mappings for loan-related types.
/// </summary>
public class LoanMapper : Profile
{
    /// <summary>
    /// Configures the mapping from <see cref="LoanEntity"/> to its overview DTO.
    /// </summary>
    public LoanMapper()
    {
        CreateMap<LoanEntity, GetLoanOverviewDto>()
            .ForMember(d => d.LoanGuid,
                o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.SpecimenName,
                o => o.MapFrom(s => s.Specimen.Name))
            .ForMember(d => d.SpecimenLatinName,
                o => o.MapFrom(s => s.Specimen.LatinName))
            .ForMember(d => d.BorrowerGuid,
                o => o.MapFrom(s => s.BorrowerGuid))
            .ForMember(d => d.BorrowerName,
                o => o.MapFrom(s => $"{s.Borrower.FirstName} {s.Borrower.LastName}"))
            .ForMember(d => d.LoanDate,
                o => o.MapFrom(s => s.LoanDate))
            .ForMember(d => d.DueDate,
                o => o.MapFrom(s => s.DueDate))
            .ForMember(d => d.ImageGuid,
                o => o.MapFrom(s => s.Specimen.ImageGuid ?? Guid.Empty));

        CreateMap<LoanEntity, GetLoanDetailDto>()
            .ForMember(d => d.LoanGuid,
                o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.SpecimenGuid,
                o => o.MapFrom(s => s.SpecimenGuid))
            .ForMember(d => d.SpecimenName,
                o => o.MapFrom(s => s.Specimen.Name))
            .ForMember(d => d.SpecimenLatinName,
                o => o.MapFrom(s => s.Specimen.LatinName))
            .ForMember(d => d.ImageGuid,
                o => o.MapFrom(s => s.Specimen.ImageGuid))
            .ForMember(d => d.BorrowerGuid,
                o => o.MapFrom(s => s.BorrowerGuid))
            .ForMember(d => d.BorrowerName,
                o => o.MapFrom(s => $"{s.Borrower.FirstName} {s.Borrower.LastName}"))
            .ForMember(d => d.BorrowerEmail,
                o => o.MapFrom(s => s.Borrower.Email))
            .ForMember(d => d.LenderGuid,
                o => o.MapFrom(s => s.LenderGuid))
            .ForMember(d => d.LenderName,
                o => o.MapFrom(s => $"{s.Lender.FirstName} {s.Lender.LastName}"))
            .ForMember(d => d.LoanDate,
                o => o.MapFrom(s => s.LoanDate))
            .ForMember(d => d.DueDate,
                o => o.MapFrom(s => s.DueDate))
            .ForMember(d => d.Notes,
                o => o.MapFrom(s => s.Notes));
    }
}
