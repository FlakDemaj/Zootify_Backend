namespace Zootify_Backend.Enums;

/// <summary>
/// The lifecycle state of a self-service loan request.
/// </summary>
public enum LoanRequestStatus
{
    /// <summary>Awaiting the required approvals (admin/moderator and, if applicable, the specimen's creator).</summary>
    Pending = 1,

    /// <summary>Fully approved; a loan has been created from the request.</summary>
    Approved = 2,

    /// <summary>Declined by an eligible approver, with a rejection reason.</summary>
    Rejected = 3
}
