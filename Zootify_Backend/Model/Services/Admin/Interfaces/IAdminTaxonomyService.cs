using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface IAdminTaxonomyService
{
    Task<GetTaxonomyTreeResponseDto> GetTaxonomiesForApprovalAsync();

    Task ApproveTaxonomyAsync(IAuthentication authentication, Guid taxonomyGuid);

    Task RejectTaxonomyAsync(IAuthentication authentication, Guid taxonomyGuid, RejectTaxonomyRequestDto request);
}
