using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

/// <summary>
/// Handles business logic for creating and returning specimen loans.
/// </summary>
public class AdminLoanService : IAdminLoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly INotificationProvider _notificationProvider;
    private readonly IPostCommitQueue _postCommitQueue;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AdminLoanService"/> with its required dependencies.
    /// </summary>
    /// <param name="loanRepository">Repository for loan persistence operations.</param>
    /// <param name="specimenRepository">Repository for specimen lookups.</param>
    /// <param name="userRepository">Repository for user lookups.</param>
    /// <param name="brevoProvider">Email provider for sending loan notification emails.</param>
    /// <param name="notificationProvider">Provider for sending in-app push notifications.</param>
    /// <param name="postCommitQueue">Queue used to dispatch notifications only after the transaction commits.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public AdminLoanService(
        ILoanRepository loanRepository,
        ISpecimenRepository specimenRepository,
        IUserRepository userRepository,
        IBrevoProvider brevoProvider,
        INotificationProvider notificationProvider,
        IPostCommitQueue postCommitQueue,
        IMapper mapper)
    {
        _loanRepository = loanRepository;
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
        _brevoProvider = brevoProvider;
        _notificationProvider = notificationProvider;
        _postCommitQueue = postCommitQueue;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns a list of active users whose names contain the given fragment, for use in borrower picker controls.
    /// </summary>
    /// <param name="filter">The name fragment to filter user names by.</param>
    /// <returns>A list of matching borrower picker DTOs.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<List<BorrowerPickerDto>> GetBorrowersAsync(string filter)
    {
        var users = await _userRepository.GetActiveUsersByNameFilterAsync(filter);

        return _mapper.Map<List<BorrowerPickerDto>>(users);
    }

    /// <summary>
    /// Returns a paged list of active (not yet returned) loans.
    /// </summary>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing the active loans and the total count.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<GetActiveLoansPagedResponseDto> GetActiveLoansAsync(BaseSortAndPagingDto paging)
    {
        var (items, total) = await _loanRepository.GetActiveLoansPagedAsync(paging);

        return new GetActiveLoansPagedResponseDto
        {
            TotalCount = total,
            Loans      = _mapper.Map<List<LoanOverviewDto>>(items),
        };
    }

    /// <summary>
    /// Returns the full detail view of the active loan with the given GUID.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    /// <returns>The detail DTO of the active loan.</returns>
    /// <exception cref="ZootifyException">Thrown when no active loan with the given GUID is found.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<LoanDetailDto> GetActiveLoanByGuidAsync(Guid loanGuid)
    {
        var loan = await _loanRepository.GetActiveLoanByGuidWithDetailsAsync(loanGuid);
        if (loan == null)
        {
            throw new ZootifyException(ErrorCode.LoanNotFound);
        }

        return _mapper.Map<LoanDetailDto>(loan);
    }

    /// <summary>
    /// Creates a new loan record, verifies the specimen and borrower exist, and sends a confirmation email to the borrower.
    /// </summary>
    /// <param name="authentication">The authentication context of the lending user.</param>
    /// <param name="request">The loan creation data including specimen, borrower, due date, and notes.</param>
    /// <returns>A DTO containing the GUID of the newly created loan.</returns>
    /// <exception cref="ZootifyException">Thrown when no specimen with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    [Transaction(ReadOnly = false)]
    public async Task<CreatedObjectResponseDto> CreateLoanAsync(
        IAuthentication authentication,
        CreateLoanRequestDto request)
    {
        var specimen = await _specimenRepository.GetSpecimenByGuidAsync(request.SpecimenGuid);
        if (specimen == null)
        {
            throw new ZootifyException(ErrorCode.NoSpecimenFound);
        }

        var borrower = await _userRepository.GetActiveUserByGuid(request.BorrowerGuid);
        if (borrower == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        var lender = await _userRepository.GetActiveUserByGuid(authentication.UserGuid);

        if (lender == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        var loan = LoanEntity.CreateLoanEntity(
            specimen.Guid,
            authentication.UserGuid,
            borrower.Guid,
            request.DueDate,
            request.Notes);

        await _loanRepository.CreateAsync(loan);

        var lenderName = $"{lender.FirstName} {lender.LastName}".Trim();

        // The borrower is notified by email and/or in-app push depending on their settings, each in
        // the borrower's preferred language. Dispatched only after the transaction commits so the
        // borrower is never notified about a loan that was rolled back.
        _postCommitQueue.Enqueue(async () =>
        {
            if (borrower.Settings.SendEmail)
            {
                await _brevoProvider.SendLoanConfirmationEmail(
                    borrower.Email,
                    $"{borrower.FirstName} {borrower.LastName}".Trim(),
                    lenderName,
                    specimen.Name,
                    specimen.LatinName,
                    loan.LoanDate,
                    loan.DueDate,
                    loan.Notes,
                    borrower.Settings.Language);
            }

            if (borrower.Settings.SendPushNotification)
            {
                await _notificationProvider.SendNotificationAsync(
                    borrower,
                    $"Eine neue Ausleihe für \"{specimen.Name}\" wurde erstellt.",
                    $"A new loan for \"{specimen.Name}\" has been created.");
            }
        });

        return new CreatedObjectResponseDto { CreatedObjectGuid = loan.Guid };
    }

    /// <summary>
    /// Marks a loan as returned, appends optional notes, and sends a return confirmation email to the borrower.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to mark as returned.</param>
    /// <param name="request">Optional notes to append to the loan record.</param>
    /// <exception cref="ZootifyException">Thrown when no loan with the given GUID is found.</exception>
    /// <exception cref="ZootifyException">Thrown when the loan has already been returned.</exception>
    [Transaction(ReadOnly = false)]
    public async Task ReturnLoanAsync(Guid loanGuid, ReturnLoanRequestDto request)
    {
        var loan = await _loanRepository.GetLoanByGuidWithDetailsAsync(loanGuid);
        if (loan == null)
        {
            throw new ZootifyException(ErrorCode.LoanNotFound);
        }

        if (loan.ReturnDate.HasValue)
        {
            throw new ZootifyException(ErrorCode.LoanAlreadyReturned);
        }

        loan.ReturnDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.AppendedNotes))
        {
            loan.Notes = string.IsNullOrWhiteSpace(loan.Notes)
                ? request.AppendedNotes
                : loan.Notes + "\n" + request.AppendedNotes;
        }

        await _loanRepository.UpdateAsync(loan);

        var borrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim();
        var returnDate = loan.ReturnDate.Value;

        // The borrower is notified by email and/or in-app push depending on their settings, each in
        // the borrower's preferred language. Dispatched only after the transaction commits.
        _postCommitQueue.Enqueue(async () =>
        {
            if (loan.Borrower.Settings.SendEmail)
            {
                await _brevoProvider.SendLoanReturnConfirmationEmail(
                    loan.Borrower.Email,
                    borrowerName,
                    loan.Specimen.Name,
                    loan.Specimen.LatinName,
                    loan.LoanDate,
                    returnDate,
                    loan.Notes,
                    loan.Borrower.Settings.Language);
            }

            if (loan.Borrower.Settings.SendPushNotification)
            {
                await _notificationProvider.SendNotificationAsync(
                    loan.Borrower,
                    $"Die Ausleihe für \"{loan.Specimen.Name}\" wurde als zurückgegeben markiert.",
                    $"The loan for \"{loan.Specimen.Name}\" has been marked as returned.");
            }
        });
    }
}
