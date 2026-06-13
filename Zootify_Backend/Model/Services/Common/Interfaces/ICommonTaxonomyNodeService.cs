using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines operations for creating and querying taxonomy nodes.
/// </summary>
public interface ICommonTaxonomyNodeService
{
    /// <summary>
    /// Creates one or more taxonomy nodes in a chain, optionally rooted under an existing parent node.
    /// </summary>
    /// <param name="authentication">The authentication context of the creating user.</param>
    /// <param name="requestDto">The taxonomy node chain to create, including an optional parent GUID.</param>
    /// <returns>A list of DTOs containing the GUIDs of the created nodes.</returns>
    Task<List<CreatedObjectResponseDto>> CreateTaxonomyNodeAsync(
        IAuthentication authentication,
        CreateTaxonomyNodeRequestDto requestDto);

    /// <summary>
    /// Returns all taxonomy nodes of the specified rank that match the given name prefix, each accompanied by their ancestor path.
    /// </summary>
    /// <param name="startsWith">Optional name prefix to filter nodes by.</param>
    /// <param name="rank">The taxonomy rank to filter by.</param>
    /// <returns>A list of taxonomy chains, where each chain runs from root to the matching node.</returns>
    Task<List<List<GetAllTaxonomyNodesByFilterDto>>> GetAllTaxonomyNodesAsync(
        string? startsWith,
        TaxonomyRank rank);

    /// <summary>
    /// Returns the full taxonomy tree, optionally filtered by a root-node name fragment.
    /// </summary>
    /// <param name="filterByName">Optional name fragment to filter root nodes by.</param>
    /// <returns>A hierarchical taxonomy tree response DTO.</returns>
    Task<GetTaxonomyTreeResponseDto> GetAllTaxonomyNodesAsTreeAsync(string? filterByName);

    /// <summary>
    /// Recursively walks up the parent chain from the given node to the root, collecting all ancestor nodes.
    /// </summary>
    /// <param name="taxonomyNode">The starting taxonomy node.</param>
    /// <param name="taxonomyNodes">The accumulator list that will contain the full ancestor chain.</param>
    /// <returns>The accumulated list of <see cref="TaxonomyNodeEntity"/> instances from leaf to root.</returns>
    Task<List<TaxonomyNodeEntity>> GetTaxonomyTreeAsync(
        TaxonomyNodeEntity taxonomyNode,
        List<TaxonomyNodeEntity> taxonomyNodes);
}