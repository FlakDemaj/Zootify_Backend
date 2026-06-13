using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
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
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AdminLoanService"/> with its required dependencies.
    /// </summary>
    /// <param name="loanRepository">Repository for loan persistence operations.</param>
    /// <param name="specimenRepository">Repository for specimen lookups.</param>
    /// <param name="userRepository">Repository for user lookups.</param>
    /// <param name="brevoProvider">Email provider for sending loan notification emails.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public AdminLoanService(
        ILoanRepository loanRepository,
        ISpecimenRepository specimenRepository,
        IUserRepository userRepository,
        IBrevoProvider brevoProvider,
        IMapper mapper)
    {
        _loanRepository = loanRepository;
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
        _brevoProvider = brevoProvider;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns a list of active users whose names start with the given prefix, for use in borrower picker controls.
    /// </summary>
    /// <param name="startsWith">The prefix to filter user names by.</param>
    /// <returns>A list of matching borrower picker DTOs.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<List<BorrowerPickerDto>> GetBorrowersAsync(string startsWith)
    {
        var users = await _userRepository.GetActiveUsersByNameStartsWithAsync(startsWith);

        return _mapper.Map<List<BorrowerPickerDto>>(users);
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
            throw new ZootifyException(ErrorCode.NoSpecimenWithThisGuid);
        }

        var borrower = await _userRepository.GetActiveUserByGuid(request.BorrowerGuid);
        if (borrower == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }

        var lender = await _userRepository.GetActiveUserByGuid(authentication.UserGuid);

        if (lender == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }

        var loan = LoanEntity.CreateLoanEntity(
            specimen.Guid,
            authentication.UserGuid,
            borrower.Guid,
            request.DueDate,
            request.Notes);

        await _loanRepository.CreateAsync(loan);

        var lenderName = $"{lender.FirstName} {lender.LastName}".Trim();

        await _brevoProvider.SendLoanConfirmationEmail(
            borrower.Email,
            $"{borrower.FirstName} {borrower.LastName}".Trim(),
            lenderName,
            specimen.Name,
            specimen.LatinName,
            loan.LoanDate,
            loan.DueDate,
            loan.Notes);

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

        await _brevoProvider.SendLoanReturnConfirmationEmail(
            loan.Borrower.Email,
            borrowerName,
            loan.Specimen.Name,
            loan.Specimen.LatinName,
            loan.LoanDate,
            loan.ReturnDate.Value,
            loan.Notes);
    }
}
