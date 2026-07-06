using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles the self-service loan-request workflow: requesting a loan (reserving the specimen and notifying
/// approvers), approving or rejecting requests, and creating the loan once fully approved.
/// </summary>
public class CommonLoanRequestService : ICommonLoanRequestService
{
    private readonly ILoanRequestRepository _loanRequestRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly INotificationProvider _notificationProvider;
    private readonly IPostCommitQueue _postCommitQueue;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonLoanRequestService"/> with its required dependencies.
    /// </summary>
    public CommonLoanRequestService(
        ILoanRequestRepository loanRequestRepository,
        ILoanRepository loanRepository,
        ISpecimenRepository specimenRepository,
        IUserRepository userRepository,
        IBrevoProvider brevoProvider,
        INotificationProvider notificationProvider,
        IPostCommitQueue postCommitQueue,
        IMapper mapper)
    {
        _loanRequestRepository = loanRequestRepository;
        _loanRepository = loanRepository;
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
        _brevoProvider = brevoProvider;
        _notificationProvider = notificationProvider;
        _postCommitQueue = postCommitQueue;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a loan request for a specimen, reserving it, and notifies all admins/moderators and the
    /// specimen's creator (except the requester).
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="request">The request data including specimen, desired due date, and optional note.</param>
    /// <returns>A DTO containing the GUID of the newly created loan request.</returns>
    /// <exception cref="ZootifyException">Thrown when the specimen is missing, not active, already requested, or already on loan.</exception>
    [Transaction(ReadOnly = false)]
    public async Task<CreatedObjectResponseDto> CreateRequestAsync(
        IAuthentication authentication,
        CreateLoanRequestRequestDto request)
    {
        var specimen = await _specimenRepository.GetSpecimenByGuidAsync(request.SpecimenGuid, includeImage: true);
        if (specimen == null)
        {
            throw new ZootifyException(ErrorCode.NoSpecimenFound);
        }

        if (specimen.Status != SpecimenStatus.Active)
        {
            throw new ZootifyException(ErrorCode.ObjectIsNotActive);
        }

        if (await _loanRequestRepository.HasOpenRequestForSpecimenAsync(specimen.Guid))
        {
            throw new ZootifyException(ErrorCode.SpecimenAlreadyRequested);
        }

        if (await _loanRepository.HasActiveLoanForSpecimenAsync(specimen.Guid))
        {
            throw new ZootifyException(ErrorCode.SpecimenAlreadyOnLoan);
        }

        var requester = await _userRepository.GetActiveUserByGuid(authentication.UserGuid);
        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        var loanRequest = LoanRequestEntity.CreateRequest(
            specimen.Guid,
            requester.Guid,
            request.DesiredDueDate,
            request.Note);

        await _loanRequestRepository.CreateAsync(loanRequest);

        var recipients = await BuildRequestRecipientsAsync(specimen.CreatedByUserGuid, requester.Guid);
        var requesterName = FullName(requester);
        var specimenName = specimen.Name;
        var specimenLatinName = specimen.LatinName;
        var desiredDueDate = request.DesiredDueDate;

        // Approvers are notified by email and/or in-app push depending on their settings, each in their own
        // language. Dispatched only after the transaction commits so nobody is notified about a request that
        // was rolled back.
        _postCommitQueue.Enqueue(() => ParallelNotifications.RunAsync(recipients, async recipient =>
        {
            if (recipient.Settings.SendEmail)
            {
                await _brevoProvider.SendLoanRequestSubmittedEmail(
                    recipient.Email, FullName(recipient), requesterName, specimenName, specimenLatinName,
                    desiredDueDate, recipient.Settings.Language);
            }

            if (recipient.Settings.SendPushNotification)
            {
                await _notificationProvider.SendNotificationAsync(
                    recipient,
                    $"Neue Ausleih-Anfrage für \"{specimenName}\" von {requesterName}.",
                    $"New loan request for \"{specimenName}\" from {requesterName}.");
            }
        }));

        return new CreatedObjectResponseDto { CreatedObjectGuid = loanRequest.Guid };
    }

    /// <summary>
    /// Returns the pending loan requests the caller still needs to decide on.
    /// </summary>
    /// <param name="authentication">The authentication context of the caller.</param>
    [Transaction(ReadOnly = true)]
    public async Task<List<GetLoanRequestOverviewDto>> GetPendingForApproverAsync(IAuthentication authentication)
    {
        var requests = await _loanRequestRepository.GetPendingForApproverAsync(authentication);

        return _mapper.Map<List<GetLoanRequestOverviewDto>>(requests);
    }

    /// <summary>
    /// Returns the detail view of a loan request the caller is allowed to see (requester, an admin/moderator,
    /// or the specimen's creator).
    /// </summary>
    /// <param name="authentication">The authentication context of the caller.</param>
    /// <param name="requestGuid">The GUID of the loan request.</param>
    /// <exception cref="ZootifyException">Thrown when the request does not exist or is not visible to the caller.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<GetLoanRequestDetailDto> GetRequestDetailAsync(IAuthentication authentication, Guid requestGuid)
    {
        var loanRequest = await _loanRequestRepository.GetByGuidWithDetailsAsync(requestGuid);
        if (loanRequest == null)
        {
            throw new ZootifyException(ErrorCode.LoanRequestNotFound);
        }

        var callerGuid = authentication.UserGuid;
        var isCallerApprover = IsAdminOrModerator(authentication)
                               || loanRequest.Specimen.CreatedByUserGuid == callerGuid;

        // Hide existence from users who are neither the requester nor an eligible approver.
        if (loanRequest.RequesterGuid != callerGuid && !isCallerApprover)
        {
            throw new ZootifyException(ErrorCode.LoanRequestNotFound);
        }

        return _mapper.Map<GetLoanRequestDetailDto>(loanRequest);
    }

    /// <summary>
    /// Registers the caller's approval of a loan request. Once all required approvals are present, a loan is
    /// created and the requester is notified.
    /// </summary>
    /// <param name="authentication">The authentication context of the approving user.</param>
    /// <param name="requestGuid">The GUID of the loan request to approve.</param>
    /// <exception cref="ZootifyException">Thrown when the request is missing, already decided, or the caller may not decide it.</exception>
    [Transaction(ReadOnly = false)]
    public async Task ApproveAsync(IAuthentication authentication, Guid requestGuid)
    {
        var loanRequest = await LoadPendingDecidableRequestAsync(authentication, requestGuid);

        var creatorGuid = loanRequest.Specimen.CreatedByUserGuid;
        var filledGate = loanRequest.RegisterApproval(
            authentication.UserGuid,
            IsAdminOrModerator(authentication),
            creatorGuid);

        if (!filledGate)
        {
            throw new ZootifyException(ErrorCode.LoanRequestAlreadyDecided);
        }

        if (loanRequest.Status != LoanRequestStatus.Approved)
        {
            await _loanRequestRepository.UpdateAsync(loanRequest);
            return;
        }

        // Fully approved: create the loan with the admin/moderator approver as lender.
        var lenderGuid = loanRequest.AdminApproverGuid!.Value;
        var loan = LoanEntity.CreateLoanEntity(
            loanRequest.SpecimenGuid,
            lenderGuid,
            loanRequest.RequesterGuid,
            loanRequest.DesiredDueDate,
            loanRequest.RequesterNote);

        await _loanRepository.CreateAsync(loan);
        loanRequest.LinkCreatedLoan(loan.Guid);
        await _loanRequestRepository.UpdateAsync(loanRequest);

        var lender = await _userRepository.GetActiveUserByGuid(lenderGuid);
        var lenderName = lender != null ? FullName(lender) : string.Empty;
        var requester = loanRequest.Requester;
        var specimenName = loanRequest.Specimen.Name;
        var specimenLatinName = loanRequest.Specimen.LatinName;
        var loanDate = loan.LoanDate;
        var dueDate = loan.DueDate;
        var notes = loan.Notes;

        // The requester (now borrower) is notified that the loan was approved, only after commit.
        _postCommitQueue.Enqueue(async () =>
        {
            if (requester.Settings.SendEmail)
            {
                await _brevoProvider.SendLoanConfirmationEmail(
                    requester.Email, FullName(requester), lenderName, specimenName, specimenLatinName,
                    loanDate, dueDate, notes, requester.Settings.Language);
            }

            if (requester.Settings.SendPushNotification)
            {
                await _notificationProvider.SendNotificationAsync(
                    requester,
                    $"Ihre Ausleih-Anfrage für \"{specimenName}\" wurde genehmigt.",
                    $"Your loan request for \"{specimenName}\" has been approved.");
            }
        });
    }

    /// <summary>
    /// Rejects a loan request with a reason and notifies the requester.
    /// </summary>
    /// <param name="authentication">The authentication context of the rejecting user.</param>
    /// <param name="requestGuid">The GUID of the loan request to reject.</param>
    /// <param name="request">The rejection reason.</param>
    /// <exception cref="ZootifyException">Thrown when the request is missing, already decided, or the caller may not decide it.</exception>
    [Transaction(ReadOnly = false)]
    public async Task RejectAsync(IAuthentication authentication, Guid requestGuid, RejectLoanRequestRequestDto request)
    {
        var loanRequest = await LoadPendingDecidableRequestAsync(authentication, requestGuid);

        loanRequest.Reject(authentication.UserGuid, request.Reason);
        await _loanRequestRepository.UpdateAsync(loanRequest);

        var requester = loanRequest.Requester;
        var specimenName = loanRequest.Specimen.Name;
        var specimenLatinName = loanRequest.Specimen.LatinName;
        var reason = request.Reason;

        // The requester is notified of the rejection (with reason), only after commit.
        _postCommitQueue.Enqueue(async () =>
        {
            if (requester.Settings.SendEmail)
            {
                await _brevoProvider.SendLoanRequestRejectedEmail(
                    requester.Email, FullName(requester), specimenName, specimenLatinName, reason,
                    requester.Settings.Language);
            }

            if (requester.Settings.SendPushNotification)
            {
                await _notificationProvider.SendNotificationAsync(
                    requester,
                    $"Ihre Ausleih-Anfrage für \"{specimenName}\" wurde abgelehnt.",
                    $"Your loan request for \"{specimenName}\" has been rejected.");
            }
        });
    }

    /// <summary>
    /// Loads a pending request and validates that the caller is an eligible approver (an admin/moderator or
    /// the specimen's creator) who is not the requester.
    /// </summary>
    /// <exception cref="ZootifyException">Thrown when the request is missing, already decided, or the caller may not decide it.</exception>
    private async Task<LoanRequestEntity> LoadPendingDecidableRequestAsync(IAuthentication authentication, Guid requestGuid)
    {
        var loanRequest = await _loanRequestRepository.GetByGuidWithDetailsAsync(requestGuid);
        if (loanRequest == null)
        {
            throw new ZootifyException(ErrorCode.LoanRequestNotFound);
        }

        if (loanRequest.Status != LoanRequestStatus.Pending)
        {
            throw new ZootifyException(ErrorCode.LoanRequestAlreadyDecided);
        }

        var callerGuid = authentication.UserGuid;
        var isEligible = callerGuid != loanRequest.RequesterGuid
                         && (IsAdminOrModerator(authentication)
                             || loanRequest.Specimen.CreatedByUserGuid == callerGuid);

        if (!isEligible)
        {
            throw new ZootifyException(ErrorCode.NotAllowedToDecideLoanRequest);
        }

        return loanRequest;
    }

    /// <summary>
    /// Builds the deduplicated notification recipient list for a new request: all active admins/moderators
    /// plus the specimen's creator, excluding the requester.
    /// </summary>
    private async Task<List<UserEntity>> BuildRequestRecipientsAsync(Guid? creatorGuid, Guid requesterGuid)
    {
        var recipients = (await _userRepository.GetAllAdminsAndModeratorUsersWithSettingsAsync())
            .Where(u => u.Guid != requesterGuid)
            .ToList();

        if (creatorGuid.HasValue
            && creatorGuid.Value != requesterGuid
            && recipients.All(u => u.Guid != creatorGuid.Value))
        {
            var creators = await _userRepository.GetUsersByGuidsAsync([creatorGuid.Value]);
            recipients.AddRange(creators);
        }

        return recipients;
    }

    /// <summary>Returns whether the caller holds an admin or moderator role.</summary>
    private static bool IsAdminOrModerator(IAuthentication authentication)
        => authentication.Role is UserRole.Admin or UserRole.Moderator;

    /// <summary>Returns the user's combined first and last name.</summary>
    private static string FullName(UserEntity user) => $"{user.FirstName} {user.LastName}".Trim();
}
