using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents a self-service request by a user to borrow a specimen. A request requires an admin/moderator
/// approval and, when the specimen has a creator different from the requester, the creator's approval as well.
/// Once fully approved a <see cref="LoanEntity"/> is created from it.
/// </summary>
public class LoanRequestEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }

    public SpecimenEntity Specimen { get; set; } = null!;

    public Guid RequesterGuid { get; set; }

    public UserEntity Requester { get; set; } = null!;

    public LoanRequestStatus Status { get; set; } = LoanRequestStatus.Pending;

    public DateTime DesiredDueDate { get; set; }

    public string? RequesterNote { get; set; }

    public DateTime RequestedAt { get; set; }

    public Guid? AdminApproverGuid { get; set; }

    public DateTime? AdminApprovedAt { get; set; }

    public Guid? CreatorApproverGuid { get; set; }

    public DateTime? CreatorApprovedAt { get; set; }

    public Guid? RejectedByGuid { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? RejectionReason { get; set; }

    public Guid? CreatedLoanGuid { get; set; }

    /// <summary>
    /// Creates a new pending loan request with the request date set to now.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen being requested.</param>
    /// <param name="requesterGuid">The GUID of the requesting user.</param>
    /// <param name="desiredDueDate">The requester's desired return date, used as the loan due date on approval.</param>
    /// <param name="note">Optional free-text note from the requester.</param>
    /// <returns>The newly created <see cref="LoanRequestEntity"/>.</returns>
    public static LoanRequestEntity CreateRequest(
        Guid specimenGuid,
        Guid requesterGuid,
        DateTime desiredDueDate,
        string? note)
    {
        return new LoanRequestEntity
        {
            SpecimenGuid   = specimenGuid,
            RequesterGuid  = requesterGuid,
            Status         = LoanRequestStatus.Pending,
            DesiredDueDate = desiredDueDate,
            RequesterNote  = note,
            RequestedAt    = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Determines whether a separate approval by the specimen's creator is required. It is required only
    /// when the specimen has a creator and that creator is not the requester (no self-approval).
    /// </summary>
    /// <param name="creatorGuid">The GUID of the specimen's creator, or <c>null</c> if none.</param>
    public bool RequiresCreatorApproval(Guid? creatorGuid)
        => creatorGuid.HasValue && creatorGuid.Value != RequesterGuid;

    /// <summary>
    /// Determines whether all required approval gates are satisfied: an admin/moderator approval is always
    /// required, plus the creator's approval when <see cref="RequiresCreatorApproval"/> holds.
    /// </summary>
    /// <param name="creatorGuid">The GUID of the specimen's creator, or <c>null</c> if none.</param>
    public bool IsFullyApproved(Guid? creatorGuid)
        => AdminApprovedAt.HasValue
           && (!RequiresCreatorApproval(creatorGuid) || CreatorApprovedAt.HasValue);

    /// <summary>
    /// Registers an approval by the given approver, filling every open gate the approver is eligible for.
    /// A creator who is also an admin/moderator fills both gates in a single call. Sets the status to
    /// <see cref="LoanRequestStatus.Approved"/> once all required gates are satisfied.
    /// </summary>
    /// <param name="approverGuid">The GUID of the approving user.</param>
    /// <param name="approverIsAdminOrModerator">Whether the approver holds an admin or moderator role.</param>
    /// <param name="creatorGuid">The GUID of the specimen's creator, or <c>null</c> if none.</param>
    /// <returns><c>true</c> if at least one open gate was filled; otherwise <c>false</c>.</returns>
    public bool RegisterApproval(Guid approverGuid, bool approverIsAdminOrModerator, Guid? creatorGuid)
    {
        var filledSomething = false;
        var now = DateTime.UtcNow;

        if (approverIsAdminOrModerator && !AdminApprovedAt.HasValue)
        {
            AdminApproverGuid = approverGuid;
            AdminApprovedAt   = now;
            filledSomething   = true;
        }

        if (RequiresCreatorApproval(creatorGuid)
            && approverGuid == creatorGuid!.Value
            && !CreatorApprovedAt.HasValue)
        {
            CreatorApproverGuid = approverGuid;
            CreatorApprovedAt   = now;
            filledSomething     = true;
        }

        if (IsFullyApproved(creatorGuid))
        {
            Status = LoanRequestStatus.Approved;
        }

        return filledSomething;
    }

    /// <summary>
    /// Marks the request as rejected, recording the approver and the rejection reason.
    /// </summary>
    /// <param name="approverGuid">The GUID of the rejecting user.</param>
    /// <param name="reason">The rejection reason shown to the requester.</param>
    public void Reject(Guid approverGuid, string reason)
    {
        Status          = LoanRequestStatus.Rejected;
        RejectedByGuid  = approverGuid;
        RejectedAt      = DateTime.UtcNow;
        RejectionReason = reason;
    }

    /// <summary>
    /// Links the loan created from this approved request.
    /// </summary>
    /// <param name="loanGuid">The GUID of the created loan.</param>
    public void LinkCreatedLoan(Guid loanGuid)
    {
        CreatedLoanGuid = loanGuid;
    }
}
