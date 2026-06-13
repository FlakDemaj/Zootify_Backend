using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines taxonomy moderation operations for approving and rejecting pending taxonomy nodes.
/// </summary>
public interface IAdminTaxonomyService
{
    /// <summary>
    /// Returns the taxonomy tree filtered to only include subtrees containing at least one pending node.
    /// </summary>
    /// <returns>A taxonomy tree response DTO containing all pending subtrees.</returns>
    Task<GetTaxonomyTreeResponseDto> GetTaxonomiesForApprovalAsync();

    /// <summary>
    /// Approves a pending taxonomy node and activates any specimens whose full taxonomy chain is now approved.
    /// </summary>
    /// <param name="authentication">The authentication context of the approving moderator or admin.</param>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to approve.</param>
    Task ApproveTaxonomyAsync(IAuthentication authentication, Guid taxonomyGuid);

    /// <summary>
    /// Rejects a taxonomy node, hard-deletes its entire subtree, rejects all associated specimens, and notifies affected users.
    /// </summary>
    /// <param name="authentication">The authentication context of the rejecting moderator or admin.</param>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to reject.</param>
    /// <param name="request">Contains the rejection comment to send to affected users.</param>
    Task RejectTaxonomyAsync(IAuthentication authentication, Guid taxonomyGuid, RejectTaxonomyRequestDto request);
}
