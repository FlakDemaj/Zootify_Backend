using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonTaxonomyNodeService
{
    Task<List<CreatedObjectResponseDto>> CreateTaxonomyNodeAsync(
        IAuthentication authentication,
        CreateTaxonomyNodeRequestDto requestDto);

    Task<List<List<GetAllTaxonomyNodesByFilterDto>>> GetAllTaxonomyNodesAsync(
        string? startsWith,
        TaxonomyRank rank);

    Task<List<TaxonomyNodeEntity>> GetTaxonomyTreeAsync(
        TaxonomyNodeEntity taxonomyNode,
        List<TaxonomyNodeEntity> taxonomyNodes);
}