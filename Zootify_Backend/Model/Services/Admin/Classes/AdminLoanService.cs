using Zootify_Backend;
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

public class AdminLoanService : IAdminLoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBrevoProvider _brevoProvider;

    public AdminLoanService(
        ILoanRepository loanRepository,
        ISpecimenRepository specimenRepository,
        IUserRepository userRepository,
        IBrevoProvider brevoProvider)
    {
        _loanRepository = loanRepository;
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
        _brevoProvider = brevoProvider;
    }

    [Transaction(ReadOnly = true)]
    public async Task<List<BorrowerPickerDto>> GetBorrowersAsync(string startsWith)
    {
        var users = await _userRepository.GetActiveUsersByNameStartsWithAsync(startsWith);

        return users
            .Select(u => new BorrowerPickerDto
            {
                UserGuid = u.Guid,
                FullName = $"{u.FirstName} {u.LastName}".Trim(),
            })
            .ToList();
    }

    [Transaction(ReadOnly = false)]
    public async Task<CreatedObjectResponseDto> CreateLoanAsync(
        IAuthentication authentication,
        CreateLoanRequestDto request)
    {
        var specimen = await _specimenRepository.GetSpecimenByGuidAsync(request.SpecimenGuid);
        if (specimen == null)
            throw new ZootifyException(ErrorCode.NoSpecimenWithThisGuid);

        var borrower = await _userRepository.GetActiveUserByGuid(request.BorrowerGuid);
        if (borrower == null)
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);

        var lender = await _userRepository.GetActiveUserByGuid(authentication.UserGuid);

        var loan = new LoanEntity
        {
            SpecimenGuid = specimen.Guid,
            LenderGuid   = authentication.UserGuid,
            BorrowerGuid = borrower.Guid,
            LoanDate     = DateTime.UtcNow,
            DueDate      = request.DueDate,
            Notes        = request.Notes,
        };

        await _loanRepository.CreateAsync(loan);

        var lenderName = lender != null
            ? $"{lender.FirstName} {lender.LastName}".Trim()
            : string.Empty;

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

    [Transaction(ReadOnly = false)]
    public async Task ReturnLoanAsync(Guid loanGuid, ReturnLoanRequestDto request)
    {
        var loan = await _loanRepository.GetLoanByGuidWithDetailsAsync(loanGuid);
        if (loan == null)
            throw new ZootifyException(ErrorCode.LoanNotFound);

        if (loan.ReturnDate.HasValue)
            throw new ZootifyException(ErrorCode.LoanAlreadyReturned);

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
