using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface IAdminLoanService
{
    Task<List<BorrowerPickerDto>> GetBorrowersAsync(string startsWith);

    Task<CreatedObjectResponseDto> CreateLoanAsync(IAuthentication authentication, CreateLoanRequestDto request);

    Task ReturnLoanAsync(Guid loanGuid, ReturnLoanRequestDto request);
}
