using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes the self-service loan-request workflow to all authenticated roles: requesting a loan, listing the
/// requests the caller must decide on, viewing a request, and approving or rejecting requests.
/// </summary>
[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator},{Roles.User}")]
public class CommonLoanRequestController : BaseRestController
{
    private readonly ICommonLoanRequestService _commonLoanRequestService;

    /// <summary>
    /// Initializes a new <see cref="CommonLoanRequestController"/> with its required service.
    /// </summary>
    /// <param name="commonLoanRequestService">The service used to manage loan requests.</param>
    public CommonLoanRequestController(ICommonLoanRequestService commonLoanRequestService)
    {
        _commonLoanRequestService = commonLoanRequestService;
    }

    /// <summary>
    /// Creates a loan request for a specimen, reserving it and notifying the approvers.
    /// </summary>
    /// <param name="request">The request data including specimen, desired due date, and optional note.</param>
    /// <returns>A DTO containing the GUID of the newly created loan request.</returns>
    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateRequestAsync(
        [FromBody] CreateLoanRequestRequestDto request)
    {
        return Ok(await _commonLoanRequestService.CreateRequestAsync(Authentication, request));
    }

    /// <summary>
    /// Returns the pending loan requests the caller still needs to decide on.
    /// </summary>
    /// <returns>A list of pending loan-request overview DTOs.</returns>
    [HttpGet("pending")]
    public async Task<ActionResult<List<LoanRequestOverviewDto>>> GetPendingAsync()
    {
        return Ok(await _commonLoanRequestService.GetPendingForApproverAsync(Authentication));
    }

    /// <summary>
    /// Returns the detail view of a loan request the caller is allowed to see.
    /// </summary>
    /// <param name="requestGuid">The GUID of the loan request.</param>
    /// <returns>The detail DTO of the loan request.</returns>
    [HttpGet("{requestGuid:guid}")]
    public async Task<ActionResult<LoanRequestDetailDto>> GetRequestAsync(Guid requestGuid)
    {
        return Ok(await _commonLoanRequestService.GetRequestDetailAsync(Authentication, requestGuid));
    }

    /// <summary>
    /// Registers the caller's approval of a loan request.
    /// </summary>
    /// <param name="requestGuid">The GUID of the loan request to approve.</param>
    [HttpPost("{requestGuid:guid}/approve")]
    public async Task<ActionResult> ApproveAsync(Guid requestGuid)
    {
        await _commonLoanRequestService.ApproveAsync(Authentication, requestGuid);
        return Ok();
    }

    /// <summary>
    /// Rejects a loan request with a reason.
    /// </summary>
    /// <param name="requestGuid">The GUID of the loan request to reject.</param>
    /// <param name="request">The rejection reason.</param>
    [HttpPost("{requestGuid:guid}/reject")]
    public async Task<ActionResult> RejectAsync(
        Guid requestGuid,
        [FromBody] RejectLoanRequestRequestDto request)
    {
        await _commonLoanRequestService.RejectAsync(Authentication, requestGuid, request);
        return Ok();
    }
}
