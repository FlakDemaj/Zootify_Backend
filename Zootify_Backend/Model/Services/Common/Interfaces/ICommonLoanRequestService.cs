using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines the self-service loan-request workflow: requesting a loan, listing pending requests for an
/// approver, viewing a request, and approving or rejecting requests.
/// </summary>
public interface ICommonLoanRequestService
{
    /// <summary>
    /// Creates a loan request for a specimen, reserving it, and notifies all admins/moderators and the
    /// specimen's creator (except the requester).
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="request">The request data including specimen, desired due date, and optional note.</param>
    /// <returns>A DTO containing the GUID of the newly created loan request.</returns>
    Task<CreatedObjectResponseDto> CreateRequestAsync(IAuthentication authentication, CreateLoanRequestRequestDto request);

    /// <summary>
    /// Returns the pending loan requests the caller still needs to decide on.
    /// </summary>
    /// <param name="authentication">The authentication context of the caller.</param>
    Task<List<LoanRequestOverviewDto>> GetPendingForApproverAsync(IAuthentication authentication);

    /// <summary>
    /// Returns the detail view of a loan request the caller is allowed to see (requester, an admin/moderator,
    /// or the specimen's creator).
    /// </summary>
    /// <param name="authentication">The authentication context of the caller.</param>
    /// <param name="requestGuid">The GUID of the loan request.</param>
    Task<LoanRequestDetailDto> GetRequestDetailAsync(IAuthentication authentication, Guid requestGuid);

    /// <summary>
    /// Registers the caller's approval of a loan request. Once all required approvals are present, a loan is
    /// created and the requester is notified.
    /// </summary>
    /// <param name="authentication">The authentication context of the approving user.</param>
    /// <param name="requestGuid">The GUID of the loan request to approve.</param>
    Task ApproveAsync(IAuthentication authentication, Guid requestGuid);

    /// <summary>
    /// Rejects a loan request with a reason and notifies the requester.
    /// </summary>
    /// <param name="authentication">The authentication context of the rejecting user.</param>
    /// <param name="requestGuid">The GUID of the loan request to reject.</param>
    /// <param name="request">The rejection reason.</param>
    Task RejectAsync(IAuthentication authentication, Guid requestGuid, RejectLoanRequestRequestDto request);
}
