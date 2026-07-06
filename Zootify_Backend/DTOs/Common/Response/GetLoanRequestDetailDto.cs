using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The full detail view of a single loan request, including specimen, requester, and decision information.
/// </summary>
public class GetLoanRequestDetailDto
{
    /// <summary>The loan request's GUID.</summary>
    public Guid RequestGuid { get; init; }

    /// <summary>The GUID of the requested specimen.</summary>
    public Guid SpecimenGuid { get; init; }

    /// <summary>The requested specimen's common display name.</summary>
    public required string SpecimenName { get; init; }

    /// <summary>The requested specimen's latin name.</summary>
    public required string SpecimenLatinName { get; init; }

    /// <summary>The GUID of the specimen's image, if any.</summary>
    public Guid? ImageGuid { get; init; }

    /// <summary>The GUID of the requesting user.</summary>
    public Guid RequesterGuid { get; init; }

    /// <summary>The requesting user's display name (first and last name combined).</summary>
    public required string RequesterName { get; init; }

    /// <summary>The requester's desired return date.</summary>
    public DateTime DesiredDueDate { get; init; }

    /// <summary>Optional free-text note from the requester.</summary>
    public string? RequesterNote { get; init; }

    /// <summary>When the request was submitted.</summary>
    public DateTime RequestedAt { get; init; }

    /// <summary>The current status of the request.</summary>
    public LoanRequestStatus Status { get; init; }

    /// <summary>Whether the admin/moderator approval has already been given.</summary>
    public bool AdminApproved { get; init; }

    /// <summary>When the admin/moderator approval was given, if any.</summary>
    public DateTime? AdminApprovedAt { get; init; }

    /// <summary>Whether a separate creator approval is required for this request.</summary>
    public bool CreatorApprovalRequired { get; init; }

    /// <summary>Whether the creator approval has already been given.</summary>
    public bool CreatorApproved { get; init; }

    /// <summary>When the creator approval was given, if any.</summary>
    public DateTime? CreatorApprovedAt { get; init; }

    /// <summary>The rejection reason, if the request was rejected.</summary>
    public string? RejectionReason { get; init; }

    /// <summary>When the request was rejected, if applicable.</summary>
    public DateTime? RejectedAt { get; init; }

    /// <summary>The GUID of the loan created from this request, once approved.</summary>
    public Guid? CreatedLoanGuid { get; init; }
}
