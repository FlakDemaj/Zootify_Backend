using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with loan-request-specific query operations.
/// </summary>
public interface ILoanRequestRepository : IBaseRepository<LoanRequestEntity>
{
    /// <summary>
    /// Returns the loan request with the given GUID, eagerly loading the specimen (including its image)
    /// and the requester (including their settings), or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the loan request to retrieve.</param>
    Task<LoanRequestEntity?> GetByGuidWithDetailsAsync(Guid guid);

    /// <summary>
    /// Returns whether the given specimen currently has an open (pending) loan request.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to check.</param>
    Task<bool> HasOpenRequestForSpecimenAsync(Guid specimenGuid);

    /// <summary>
    /// Returns the pending loan requests the given caller still needs to decide on. Admins and moderators
    /// see all requests still awaiting an admin/moderator approval; a regular user (or an admin/moderator
    /// acting as the specimen's creator) sees requests for specimens they created that still await the
    /// creator's approval. The caller never sees their own requests.
    /// </summary>
    /// <param name="authentication">The authentication context of the caller, used for the role-based filter.</param>
    Task<List<LoanRequestEntity>> GetPendingForApproverAsync(IAuthentication authentication);
}
