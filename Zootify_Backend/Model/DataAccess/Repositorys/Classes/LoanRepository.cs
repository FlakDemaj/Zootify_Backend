using Microsoft.EntityFrameworkCore;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="LoanEntity"/> records.
/// </summary>
public class LoanRepository : BaseRepository<LoanEntity>, ILoanRepository
{
    /// <summary>
    /// Initializes a new <see cref="LoanRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public LoanRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns a paged list of active (not yet returned) loans, eagerly loading borrower and specimen details.
    /// Admins and moderators see all active loans; regular users only see loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user, used for visibility filtering.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A tuple containing the page of active loans and the total matching count.</returns>
    public async Task<(List<LoanEntity> Items, int TotalCount)> GetActiveLoansPagedAsync(
        IAuthentication authentication,
        BaseSortAndPagingDto paging)
    {
        // Only to-one navigations are included, so paging stays a single query.
        var query = Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower)
            .Include(l => l.Specimen).ThenInclude(s => s.Image)
            .Where(l => l.ReturnDate == null);

        // Regular users only see loans of specimens they created; moderators and admins see all.
        if (authentication.Role == UserRole.User)
        {
            query = query.Where(l => l.Specimen.CreatedByUserGuid == authentication.UserGuid);
        }

        var total = await query.CountAsync();

        var items = await ApplySort(query, paging.SortBy, paging.SortDirection)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Returns the active (not yet returned) loan with the given GUID, eagerly loading borrower, lender,
    /// and specimen (including its image) details, or <c>null</c> if no matching active loan is visible to the caller.
    /// Admins and moderators may see any active loan; regular users only see loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user, used for visibility filtering.</param>
    /// <param name="guid">The GUID of the loan to retrieve.</param>
    public async Task<LoanEntity?> GetActiveLoanByGuidWithDetailsAsync(IAuthentication authentication, Guid guid)
    {
        var query = Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower)
            .Include(l => l.Lender)
            .Include(l => l.Specimen).ThenInclude(s => s.Image)
            .Where(l => l.ReturnDate == null);

        // Regular users only see loans of specimens they created; moderators and admins see all.
        if (authentication.Role == UserRole.User)
        {
            query = query.Where(l => l.Specimen.CreatedByUserGuid == authentication.UserGuid);
        }

        return await query.FirstOrDefaultAsync(l => l.Guid == guid);
    }

    /// <summary>
    /// Returns whether the given specimen currently has an active (not yet returned) loan.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to check.</param>
    public async Task<bool> HasActiveLoanForSpecimenAsync(Guid specimenGuid)
    {
        return await Context.GetTrackingSet<LoanEntity>()
            .AnyAsync(l => l.SpecimenGuid == specimenGuid && l.ReturnDate == null);
    }

    /// <summary>
    /// Returns the active (not yet returned) loans the given user has borrowed, eagerly loading specimen
    /// (including its image) and borrower details, ordered by due date.
    /// </summary>
    /// <param name="borrowerGuid">The GUID of the borrowing user.</param>
    public async Task<List<LoanEntity>> GetActiveBorrowedLoansAsync(Guid borrowerGuid)
    {
        return await Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower)
            .Include(l => l.Specimen).ThenInclude(s => s.Image)
            .Where(l => l.BorrowerGuid == borrowerGuid && l.ReturnDate == null)
            .OrderBy(l => l.DueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Returns the active (not yet returned) loan with the given GUID borrowed by the given user, eagerly
    /// loading borrower (including their settings) and specimen details, or <c>null</c> if no such loan exists.
    /// </summary>
    /// <param name="borrowerGuid">The GUID of the borrowing user.</param>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    public async Task<LoanEntity?> GetActiveBorrowedLoanByGuidAsync(Guid borrowerGuid, Guid loanGuid)
    {
        return await Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower).ThenInclude(b => b.Settings)
            .Include(l => l.Specimen)
            .FirstOrDefaultAsync(l => l.Guid == loanGuid
                                      && l.BorrowerGuid == borrowerGuid
                                      && l.ReturnDate == null);
    }

    /// <summary>
    /// Returns a batch of unreturned loans due before the given date that have not yet had a stage-1 reminder sent.
    /// </summary>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    public async Task<List<LoanEntity>> GetLoansForStage1ReminderAsync(DateTime dueFrom, DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage1ReminderSentAt == null
                        && l.DueDate >= dueFrom
                        && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a batch of unreturned loans due before the given date that have not yet had a stage-2 reminder sent.
    /// </summary>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    public async Task<List<LoanEntity>> GetLoansForStage2ReminderAsync(DateTime dueFrom, DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage2ReminderSentAt == null
                        && l.DueDate >= dueFrom
                        && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a batch of unreturned loans due before the given date that have not yet had a stage-3 reminder sent.
    /// </summary>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    public async Task<List<LoanEntity>> GetLoansForStage3ReminderAsync(DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage3ReminderSentAt == null && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Builds the base query for unreturned loans with borrower, lender (including their settings),
    /// and specimen eagerly loaded, ordered by due date. The settings are needed so each reminder
    /// can be sent in the recipient's preferred language.
    /// </summary>
    /// <returns>The base <see cref="IQueryable{LoanEntity}"/> for reminder queries.</returns>
    private IQueryable<LoanEntity> ReminderQuery()
    {
        return Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower).ThenInclude(b => b.Settings)
            .Include(l => l.Lender).ThenInclude(l => l.Settings)
            .Include(l => l.Specimen)
            .Where(l => l.ReturnDate == null)
            .OrderBy(l => l.DueDate);
    }

    /// <summary>
    /// Applies ordering to the loan query based on the given sort field and direction.
    /// </summary>
    /// <param name="query">The queryable to sort.</param>
    /// <param name="sortBy">The name of the field to sort by.</param>
    /// <param name="sortDirection">The sort direction.</param>
    /// <returns>The ordered queryable.</returns>
    private static IQueryable<LoanEntity> ApplySort(
        IQueryable<LoanEntity> query,
        string? sortBy,
        SortDirection sortDirection)
    {
        var desc = sortDirection == SortDirection.Desc;
        return sortBy switch
        {
            "specimenName" => desc ? query.OrderByDescending(l => l.Specimen.Name)      : query.OrderBy(l => l.Specimen.Name),
            "borrowerName" => desc ? query.OrderByDescending(l => l.Borrower.FirstName).ThenByDescending(l => l.Borrower.LastName)
                                   : query.OrderBy(l => l.Borrower.FirstName).ThenBy(l => l.Borrower.LastName),
            "loanDate"     => desc ? query.OrderByDescending(l => l.LoanDate)            : query.OrderBy(l => l.LoanDate),
            "dueDate"      => desc ? query.OrderByDescending(l => l.DueDate)             : query.OrderBy(l => l.DueDate),
            _              => query.OrderBy(l => l.DueDate),
        };
    }
}
